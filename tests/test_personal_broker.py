import importlib.util
import os
import pathlib
import shutil
import struct
import subprocess
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location('broker_image', ROOT / 'scripts/personal_broker_image.py')
IMAGE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(IMAGE)


def native_image(payload=b'sealed payload', manifest=b'<assembly>asInvoker</assembly>'):
    resource = bytearray()

    def directory(tree):
        start = len(resource)
        resource.extend(bytes(16 + 8 * len(tree)))
        struct.pack_into('<H', resource, start + 14, len(tree))
        for i, (name, value) in enumerate(sorted(tree.items())):
            if isinstance(value, dict):
                target = directory(value) | 0x80000000
            else:
                target = len(resource)
                resource.extend(bytes(16))
                offset = len(resource)
                resource.extend(value)
                resource.extend(bytes((-len(resource)) % 4))
                struct.pack_into('<II', resource, target, 0x3000 + offset, len(value))
            struct.pack_into('<II', resource, start + 16 + i * 8, name, target)
        return start

    directory({10: {101: {1033: payload}, 102: {1033: b'signature'}}, 24: {1: {1033: manifest}}})
    data = bytearray(2048)
    data[:2] = b'MZ'
    struct.pack_into('<I', data, 60, 128)
    data[128:132] = b'PE\0\0'
    struct.pack_into('<HH', data, 132, 0x8664, 3)
    struct.pack_into('<H', data, 148, 240)
    opt = 152
    struct.pack_into('<H', data, opt, 0x20b)
    struct.pack_into('<I', data, opt + 108, 16)
    struct.pack_into('<II', data, opt + 128, 0x3000, len(resource))
    for i, (name, va, raw, flags) in enumerate(((b'.text', 0x1000, 512, 0x60000020),
                                               (b'.rdata', 0x2000, 1024, 0x40000040),
                                               (b'.rsrc', 0x3000, 1536, 0x40000040))):
        off = opt + 240 + i * 40
        data[off:off + 8] = name.ljust(8, b'\0')
        struct.pack_into('<IIII', data, off + 8, 512, va, 512, raw)
        struct.pack_into('<I', data, off + 36, flags)
    data[512] = 0xc3
    data[1024:1034] = b'public-key'
    data[1536:1536 + len(resource)] = resource
    return data


class PersonalBrokerTests(unittest.TestCase):
    def test_native_identity_binds_code_key_manifest_and_loader_header(self):
        original = native_image()
        digest, resources = IMAGE.inspect(original)
        self.assertEqual(resources(10, 101), b'sealed payload')
        self.assertEqual(digest, IMAGE.inspect(native_image(b'new sealed payload'))[0])
        for offset in (512, 1024, 152 + 16):
            changed = bytearray(original)
            changed[offset] ^= 1
            self.assertNotEqual(digest, IMAGE.inspect(changed)[0])
        self.assertNotEqual(digest, IMAGE.inspect(native_image(manifest=b'<assembly>foreign</assembly>'))[0])

    def test_relocation_directory_cannot_escape_hashed_section(self):
        original = native_image()
        changed = bytearray(original)
        struct.pack_into('<II', changed, 152+152, 0x31e0, 12)
        struct.pack_into('<IIHH', changed, 1536+0x1e0, 0x1000, 12, 0xa010, 0)
        with self.assertRaises(ValueError):
            IMAGE.inspect(changed)
        # A real bounded relocation section can move when UpdateResource grows
        # resources; its directory length and bytes remain part of identity.
        original.extend(bytes(1024))
        original[2560:3072] = original[512:1024]
        struct.pack_into('<I', original, 152+240+20, 2560)
        struct.pack_into('<H', original, 134, 4)
        off = 152+240+3*40
        original[off:off+8] = b'.reloc\0\0'
        struct.pack_into('<IIII', original, off+8, 12, 0x4000, 512, 2048)
        struct.pack_into('<I', original, off+36, 0x42000040)
        struct.pack_into('<II', original, 152+152, 0x4000, 12)
        struct.pack_into('<IIHH', original, 2048, 0x1000, 12, 0xa010, 0)
        digest = IMAGE.inspect(original)[0]
        moved = bytearray(original)
        struct.pack_into('<I', moved, off+12, 0x5000)
        struct.pack_into('<I', moved, 152+152, 0x5000)
        self.assertEqual(digest, IMAGE.inspect(moved)[0])
        # Model resource growth plus raw/RVA relocation placement by UpdateResource.
        moved.extend(original[2048:2560])
        moved[2048:2560] = bytes(512)
        struct.pack_into('<I', moved, off+20, 3072)
        resource_section = 152+240+2*40
        struct.pack_into('<I', moved, resource_section+8, 1024)
        struct.pack_into('<I', moved, resource_section+16, 1024)
        struct.pack_into('<I', moved, 152+132, 1024)
        self.assertEqual(digest, IMAGE.inspect(moved)[0])
        for directory in ((0x31e0,12),(0x4002,10),(0x4000,13),(0,0)):
            changed = bytearray(original)
            struct.pack_into('<II', changed, 152+152, *directory)
            with self.assertRaises(ValueError): IMAGE.inspect(changed)
        changed = bytearray(original)
        struct.pack_into('<I', changed, 152+156, 10)
        self.assertNotEqual(digest, IMAGE.inspect(changed)[0])
        changed = bytearray(original); changed[2056] ^= 1
        self.assertNotEqual(digest, IMAGE.inspect(changed)[0])

    def test_managed_tls_executable_resources_overlap_and_truncation_are_refused(self):
        for offset, value in ((152 + 224, 1), (152 + 184, 1), (152 + 144, 1),
                              (152 + 240 + 2 * 40 + 36, 0x60000040),
                              (152 + 240 + 2 * 40 + 20, 512)):
            changed = native_image()
            struct.pack_into('<I', changed, offset, value)
            with self.assertRaises(ValueError):
                IMAGE.inspect(changed)
        image = native_image()
        for size in (0, 64, 200, 600, len(image)-1):
            with self.assertRaises(ValueError):
                IMAGE.inspect(image[:size])

    def test_actual_portable_launcher_policy_under_hostile_environment(self):
        compiler = shutil.which('cc')
        if compiler is None:
            self.skipTest('portable native compiler unavailable')
        harness = r'''
#include <stdio.h>
#include "personal_broker_policy.h"
static int check(int ok,int line) { if(!ok) fprintf(stderr,"policy failure %d\n",line); return ok; }
#define C(x) do { if(!check((x),__LINE__)) return 1; } while(0)
int main(void) {
    unsigned i; wchar_t env[4096]; pg_exchange x={0,0,0,0};
    C(pg_fixture_leaf(L"ProGo-native-broker-fixture-0123456789abcdef0123456789abcdef"));
    C(!pg_fixture_leaf(L"ProGo-native-broker-fixture-0123456789abcdef0123456789abcdeg"));
    C(!pg_fixture_leaf(L"ProGo-native-broker-fixture-0123456789abcdef0123456789abcde"));
    C(!pg_fixture_leaf(L"ProGo-native-broker-fixture-0123456789abcdef0123456789abcdef0"));
    C(!pg_fixture_leaf(L"ProGo-native-broker-fixture-0123456789abcdef0123456789abcdeF"));
    C(pg_route(L"\\\\.\\pipe\\ProGo-personal-0123456789abcdef0123456789abcdef"));
    C(!pg_route(L"\\\\remote\\pipe\\ProGo-personal-0123456789abcdef0123456789abcdef"));
    C(!pg_route(L"\\\\.\\pipe\\ProGo-personal-0123456789abcdef0123456789abcdeg"));
    for(i=0;i<6;i++) {
        int facts[6]={1,1,1,1,1,1}; facts[i]=0;
        C(!pg_admit(&x,PG_INSTALL,facts[0],facts[1],facts[2],facts[3],facts[4],facts[5]));
        C(x.phase==0 && x.claimed==0);
    }
    C(!pg_admit(&x,3,1,1,1,1,1,1)); C(!pg_claim(&x,PG_INSTALL)); C(!pg_settle(&x,0));
    C(pg_admit(&x,PG_INSTALL,1,1,1,1,1,1)); C(!pg_admit(&x,PG_INSTALL,1,1,1,1,1,1));
    C(!pg_claim(&x,PG_UNINSTALL)); C(pg_claim(&x,PG_INSTALL)); C(!pg_claim(&x,PG_INSTALL));
    C(pg_heartbeat(&x,1,1)); C(!pg_heartbeat(&x,0,1)); C(!pg_heartbeat(&x,1,0));
    C(!pg_settle(&x,2)); C(pg_settle(&x,0)); C(!pg_settle(&x,0)); C(!pg_heartbeat(&x,1,1));
    C(pg_environment(env,4096,L"C:\\Windows",L"C:\\Windows\\System32",L"C:\\fixture\\scratch"));
    for(i=0;env[i] || env[i+1];i++) putchar(env[i]?(char)env[i]:'\n');
    C(!pg_environment(env,4,L"C:\\Windows",L"C:\\Windows\\System32",L"C:\\fixture"));
    C(!pg_environment(env,4096,L"C:\\Windows",L"C:\\Windows\\System32",L"bad\npath"));
    return 0;
}
'''
        with tempfile.TemporaryDirectory(prefix='ProGo-native-policy-') as temp:
            source = pathlib.Path(temp) / 'policy.c'
            executable = pathlib.Path(temp) / 'policy'
            source.write_text(harness)
            subprocess.run([compiler, '-std=c11', '-Wall', '-Wextra', '-Werror', '-I', str(ROOT/'native'),
                            str(source), '-o', str(executable)], check=True, capture_output=True, text=True)
            environment = dict(os.environ, APPDOMAIN_MANAGER_ASM='foreign', APPDOMAIN_MANAGER_TYPE='foreign',
                               COR_ENABLE_PROFILING='1', COR_PROFILER_PATH='foreign', COMPLUS_Version='foreign',
                               DOTNET_STARTUP_HOOKS='foreign')
            result = subprocess.run([str(executable)], env=environment, check=True, capture_output=True, text=True)
        rows = result.stdout.splitlines()
        self.assertEqual(rows, ['PATH=C:\\Windows\\System32',
                                'PSModulePath=C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\Modules',
                                'SystemRoot=C:\\Windows', 'TEMP=C:\\fixture\\scratch',
                                'TMP=C:\\fixture\\scratch', 'WINDIR=C:\\Windows'])

    def test_consumed_factory_bootstrap_and_fixture_boundaries(self):
        native = (ROOT/'native/personal_broker.c').read_text()
        managed = (ROOT/'src/PersonalBroker.cs').read_text()
        driver = (ROOT/'scripts/PersonalBroker.ps1').read_text(encoding='utf-8-sig')
        install = (ROOT/'scripts/Install-ProGo.ps1').read_text(encoding='utf-8-sig')
        uninstall = (ROOT/'scripts/Uninstall-ProGo.ps1').read_text(encoding='utf-8-sig')
        self.assertIn('::OpenPersonalInstallerTransaction()', install)
        self.assertIn('::OpenPersonalUninstallerTransaction()', uninstall)
        self.assertIn('EnterNativeBroker($BrokerPipe)', driver)
        self.assertIn('-Personal -NoStartup -PayloadDirectory $PSScriptRoot', driver)
        self.assertIn('CreateProcessW(engine,command,NULL,NULL,FALSE,CREATE_SUSPENDED|CREATE_UNICODE_ENVIRONMENT', native)
        self.assertIn('AssignProcessToJobObject(g.job,pi->hProcess)', native)
        self.assertIn('pg_start_host(script,g.system,scratch,arguments,&pi)', native)
        imports = (ROOT/'src/ApplicationShortcuts.cs').read_text(encoding='utf-8-sig')
        self.assertEqual(imports.count('[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]'), 3)
        self.assertEqual(imports.count('[DllImport("ktmw32.dll",'), 3)
        launch = (ROOT/'tests/NativeBrokerLaunchTests.ps1').read_text(encoding='utf-8-sig')
        self.assertEqual(launch.count('$unknownTree = $false'), 1)
        self.assertNotIn('$script:unknownTree = $false', launch)
        self.assertIn('if (-not $script:unknownTree -and', launch)
        self.assertIn('later normal exit cannot clear unknown tree', launch)
        self.assertIn('timeout and launcher kill/join retain unknown tree', launch)
        self.assertNotIn('ExecutionPolicy', driver)
        self.assertNotIn('GetEnvironmentVariable', managed)
        for token in ('BrokerServerPid(pipe.SafePipeHandle', 'native.Digest()', 'rsa.VerifyData',
                      'BrokerLinked(workerToken,originalToken)', 'BrokerLinked(currentToken,originalToken)',
                      'transaction.HasSettledRegistration', 'transaction.disposed', 'new NativeRegistrationStore'):
            self.assertIn(token, managed)
        tests = (ROOT/'scripts/Test-ProGo.ps1').read_text(encoding='utf-8-sig')
        for fixture in ('NativeBrokerLaunchTests.ps1','PersonalBrokerTests.ps1','NativeRegistrationTransactionTests.ps1'):
            self.assertEqual(tests.count("'tests\\"+fixture+"'"), 1)


if __name__ == '__main__':
    unittest.main()
