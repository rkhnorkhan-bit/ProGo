/* Native UAC bootstrap. No CLR, PowerShell, application DLL or inherited CLR
 * configuration executes in this image. Only kernel32 and the static CRT are
 * imported; optional OS libraries are resolved by absolute System32 paths.
 * The initial, user-selected native image and normal Windows UAC consent are
 * the trust root. The ephemeral build seal binds its payload, not its publisher.
 */
#define UNICODE
#define _UNICODE
#define WIN32_LEAN_AND_MEAN
#ifndef _WIN32_WINNT
#define _WIN32_WINNT 0x0A00
#endif
#ifndef WINVER
#define WINVER _WIN32_WINNT
#endif
#include <windows.h>
#include <shellapi.h>
#include <shlobj.h>
#include <bcrypt.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "personal_broker_policy.h"
#include "broker_key.h"

typedef struct pg_native {
    HANDLE image, original, original_token, self_token, client, pipe, job;
    HANDLE pins[256]; unsigned pin_count;
    HANDLE roots[4]; wchar_t root_paths[4][32768];
    wchar_t *directories[256]; unsigned directory_count;
    wchar_t self[32768], system[32768], windows[32768], runtime[32768], data[32768], payload[32768];
    DWORD original_pid;
    ULONGLONG deadline;
    pg_exchange exchange;
} pg_native;
static pg_native g;
/* Numeric-only fixture telemetry; never an admission input. Stage values are stable. */
#ifdef PROGO_BROKER_NATIVE_FIXTURE
typedef enum pg_diag_stage {
    PG_D_SYSTEM=1, PG_D_WINDOWS, PG_D_DLL_POLICY, PG_D_MODULE, PG_D_LOCAL_IMAGE,
    PG_D_IMAGE, PG_D_TOKEN, PG_D_SHELL, PG_D_SPLIT, PG_D_ARGV,
    PG_D_FIXTURE_ROOT, PG_D_TEMP, PG_D_TEMP_IDENTITY, PG_D_SCRATCH,
    PG_D_COMMAND, PG_D_ENGINE, PG_D_ENV_ALLOC, PG_D_ENV, PG_D_JOB_CREATE,
    PG_D_JOB_LIMIT, PG_D_CREATE, PG_D_ASSIGN, PG_D_RESUME, PG_D_WAIT,
    PG_D_EXIT, PG_D_ACCOUNT, PG_D_DONE
} pg_diag_stage;
static struct { unsigned stage; DWORD error; int temp_match,long_known,long_match,identity_result;
    int created,assigned,resumed,exit_known,account_ok; DWORD host_exit,active,wait_result; } pg_diag;
#define PG_STAGE(x) do { pg_diag.stage=(x); SetLastError(ERROR_SUCCESS); } while(0)
#define PG_ERROR() do { pg_diag.error=GetLastError(); } while(0)
static void pg_diag_write(void) {
    char line[512]; DWORD written; int n;
    n=sprintf_s(line,sizeof(line),"PG_NATIVE_DIAG v=2 stage=%u error=%lu temp_match=%d long_known=%d long_match=%d created=%d assigned=%d resumed=%d exit_known=%d host_exit=%lu account_ok=%d active=%lu wait_result=%lu identity_result=%d\n",
        pg_diag.stage,pg_diag.error,pg_diag.temp_match,pg_diag.long_known,pg_diag.long_match,
        pg_diag.created,pg_diag.assigned,pg_diag.resumed,pg_diag.exit_known,pg_diag.host_exit,pg_diag.account_ok,pg_diag.active,pg_diag.wait_result,pg_diag.identity_result);
    if(n>0) WriteFile(GetStdHandle(STD_OUTPUT_HANDLE),line,(DWORD)n,&written,NULL);
}
#define PG_FLAG(x) do { pg_diag.x=1; } while(0)
#else
#define PG_STAGE(x) ((void)0)
#define PG_ERROR() ((void)0)
#define PG_FLAG(x) ((void)0)
#endif
static const GUID pg_programs = {0x905e63b6,0xc1bf,0x494e,{0xb2,0x9c,0x65,0xb7,0x32,0xd3,0xd2,0x1a}};
static const GUID pg_local = {0xf1b32785,0x6fba,0x4fcf,{0x9d,0x55,0x7b,0x8e,0x7f,0x15,0x70,0x91}};
static const GUID pg_menu = {0xa77f5d77,0x2e2b,0x44c3,{0xa6,0xa2,0xab,0xa6,0x01,0x05,0x4a,0x51}};
static const GUID pg_startup = {0xb97d20bb,0xf46a,0x4c97,{0xba,0x10,0x5e,0x36,0x08,0x43,0x08,0x54}};
static HMODULE pg_system_library(const wchar_t *leaf) {
    wchar_t path[32768];
    if (swprintf_s(path,32768,L"%s\\%s",g.system,leaf) < 0) return NULL;
    return LoadLibraryExW(path,NULL,LOAD_LIBRARY_SEARCH_SYSTEM32);
}
static int pg_hold(HANDLE h) {
    if (!h || h == INVALID_HANDLE_VALUE || g.pin_count == 256) { if (h && h != INVALID_HANDLE_VALUE) CloseHandle(h); return 0; }
    g.pins[g.pin_count++] = h; return 1;
}
static int pg_local_path(const wchar_t *path) {
    wchar_t drive[4],device[32768];
    if(!path || wcslen(path)<3 || path[1]!=L':' || path[2]!=L'\\') return 0;
    drive[0]=path[0];drive[1]=L':';drive[2]=0;
    if(!QueryDosDeviceW(drive,device,32768)) { PG_ERROR(); return 0; }
    if(wcsncmp(device,L"\\Device\\HarddiskVolume",22)) return 0;
    drive[2]=L'\\';drive[3]=0;return GetDriveTypeW(drive)==DRIVE_FIXED;
}
static HANDLE pg_open(const wchar_t *path, int directory) {
    HANDLE h = CreateFileW(path,directory?FILE_READ_ATTRIBUTES:GENERIC_READ,FILE_SHARE_READ,NULL,OPEN_EXISTING,
        FILE_FLAG_OPEN_REPARSE_POINT | (directory ? FILE_FLAG_BACKUP_SEMANTICS : 0),NULL);
    BY_HANDLE_FILE_INFORMATION info;
    if (h == INVALID_HANDLE_VALUE) { PG_ERROR(); return NULL; }
    if (!GetFileInformationByHandle(h,&info)) { PG_ERROR(); CloseHandle(h); return NULL; }
    if ((info.dwFileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) ||
        (!!(info.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) != !!directory)) { CloseHandle(h); return NULL; }
    return h;
}
static int pg_same_file(HANDLE a,HANDLE b) {
    BY_HANDLE_FILE_INFORMATION x,y;
    if(!a || !b) return 0;
    if(!GetFileInformationByHandle(a,&x)) { PG_ERROR(); return 0; }
    if(!GetFileInformationByHandle(b,&y)) { PG_ERROR(); return 0; }
    return x.dwVolumeSerialNumber == y.dwVolumeSerialNumber && x.nFileIndexHigh == y.nFileIndexHigh && x.nFileIndexLow == y.nFileIndexLow &&
        x.ftCreationTime.dwHighDateTime==y.ftCreationTime.dwHighDateTime && x.ftCreationTime.dwLowDateTime==y.ftCreationTime.dwLowDateTime;
}
static int pg_pin_tree(wchar_t *path,int create) {
    size_t i,n=wcslen(path); wchar_t saved; unsigned k;
    if (!pg_local_path(path)) return 0;
    for (i=3;i<=n;++i) if (path[i] == L'\\' || path[i] == 0) {
        saved=path[i]; path[i]=0;
        for(k=0;k<g.directory_count;++k) if(!_wcsicmp(path,g.directories[k])) break;
        if(k<g.directory_count) { path[i]=saved; continue; }
        if (create && !CreateDirectoryW(path,NULL) && GetLastError()!=ERROR_ALREADY_EXISTS) { PG_ERROR(); path[i]=saved; return 0; }
        if (!pg_hold(pg_open(path,1))) { path[i]=saved; return 0; }
        if(g.directory_count==256) { path[i]=saved; return 0; }
        g.directories[g.directory_count]=(wchar_t*)HeapAlloc(GetProcessHeap(),0,(i+1)*sizeof(wchar_t));
        if(!g.directories[g.directory_count]) { path[i]=saved; return 0; }
        memcpy(g.directories[g.directory_count++],path,(i+1)*sizeof(wchar_t));
        path[i]=saved;
    }
    return 1;
}
static int pg_token(HANDLE process,DWORD access,HANDLE *token) {
    typedef BOOL (WINAPI *open_fn)(HANDLE,DWORD,PHANDLE);
    HMODULE a=pg_system_library(L"advapi32.dll"); open_fn fn;
    if (!a) return 0; fn=(open_fn)GetProcAddress(a,"OpenProcessToken");
    return fn && fn(process,access,token);
}
static int pg_info(HANDLE token,TOKEN_INFORMATION_CLASS kind,void *out,DWORD size) {
    typedef BOOL (WINAPI *info_fn)(HANDLE,TOKEN_INFORMATION_CLASS,LPVOID,DWORD,PDWORD);
    HMODULE a=pg_system_library(L"advapi32.dll"); info_fn fn; DWORD count=0;
    if (!a) return 0; fn=(info_fn)GetProcAddress(a,"GetTokenInformation");
    return fn && fn(token,kind,out,size,&count);
}
static int pg_same_user(HANDLE a,HANDLE b) {
    BYTE one[1024],two[1024];
    typedef BOOL (WINAPI *sid_fn)(PSID,PSID); HMODULE lib=pg_system_library(L"advapi32.dll"); sid_fn eq;
    if (!lib) return 0; eq=(sid_fn)GetProcAddress(lib,"EqualSid");
    return eq && pg_info(a,TokenUser,one,sizeof(one)) && pg_info(b,TokenUser,two,sizeof(two)) && eq(((TOKEN_USER*)one)->User.Sid,((TOKEN_USER*)two)->User.Sid);
}
static int pg_linked(HANDLE elevated,HANDLE limited) {
    TOKEN_ELEVATION_TYPE a,b; TOKEN_LINKED_TOKEN link; TOKEN_STATISTICS x,y;
    DWORD sa,sb; int ok=0;
    if (!pg_info(elevated,TokenElevationType,&a,sizeof(a)) || a!=TokenElevationTypeFull ||
        !pg_info(limited,TokenElevationType,&b,sizeof(b)) || b!=TokenElevationTypeLimited ||
        !pg_same_user(elevated,limited) || !pg_info(elevated,TokenSessionId,&sa,sizeof(sa)) ||
        !pg_info(limited,TokenSessionId,&sb,sizeof(sb)) || sa!=sb ||
        !pg_info(elevated,TokenLinkedToken,&link,sizeof(link))) return 0;
    ok=pg_info(link.LinkedToken,TokenStatistics,&x,sizeof(x)) && pg_info(limited,TokenStatistics,&y,sizeof(y)) &&
        x.AuthenticationId.LowPart==y.AuthenticationId.LowPart && x.AuthenticationId.HighPart==y.AuthenticationId.HighPart;
    CloseHandle(link.LinkedToken); return ok;
}
static int pg_image(HANDLE process) {
    wchar_t path[32768]; DWORD n=32768; HANDLE image; int ok;
    if (!QueryFullProcessImageNameW(process,0,path,&n) || !pg_local_path(path)) return 0;
    image=pg_open(path,0); ok=pg_same_file(g.image,image); if (image) CloseHandle(image); return ok;
}
static int pg_random(BYTE *out,ULONG n) {
    typedef NTSTATUS (WINAPI *random_fn)(BCRYPT_ALG_HANDLE,PUCHAR,ULONG,ULONG);
    HMODULE b=pg_system_library(L"bcrypt.dll"); random_fn fn;
    if (!b) return 0; fn=(random_fn)GetProcAddress(b,"BCryptGenRandom");
    return fn && fn(NULL,out,n,BCRYPT_USE_SYSTEM_PREFERRED_RNG)>=0;
}
static int pg_verify(const BYTE *bytes,DWORD length,const BYTE *signature,DWORD signature_length) {
    typedef NTSTATUS (WINAPI *open_fn)(BCRYPT_ALG_HANDLE*,LPCWSTR,LPCWSTR,ULONG);
    typedef NTSTATUS (WINAPI *hash_fn)(BCRYPT_ALG_HANDLE,PUCHAR,ULONG,PUCHAR,ULONG,PUCHAR,ULONG);
    typedef NTSTATUS (WINAPI *import_fn)(BCRYPT_ALG_HANDLE,BCRYPT_KEY_HANDLE,LPCWSTR,BCRYPT_KEY_HANDLE*,PUCHAR,ULONG,ULONG);
    typedef NTSTATUS (WINAPI *verify_fn)(BCRYPT_KEY_HANDLE,VOID*,PUCHAR,ULONG,PUCHAR,ULONG,ULONG);
    typedef NTSTATUS (WINAPI *close_fn)(BCRYPT_ALG_HANDLE,ULONG);
    typedef NTSTATUS (WINAPI *destroy_fn)(BCRYPT_KEY_HANDLE);
    HMODULE b=pg_system_library(L"bcrypt.dll"); BCRYPT_ALG_HANDLE sha=NULL,rsa=NULL; BCRYPT_KEY_HANDLE key=NULL;
    BYTE digest[32]; BCRYPT_PKCS1_PADDING_INFO padding={BCRYPT_SHA256_ALGORITHM}; int ok=0;
    open_fn open; hash_fn hash; import_fn import_key; verify_fn verify; close_fn close; destroy_fn destroy;
    if (!b) return 0;
    open=(open_fn)GetProcAddress(b,"BCryptOpenAlgorithmProvider"); hash=(hash_fn)GetProcAddress(b,"BCryptHash");
    import_key=(import_fn)GetProcAddress(b,"BCryptImportKeyPair"); verify=(verify_fn)GetProcAddress(b,"BCryptVerifySignature");
    close=(close_fn)GetProcAddress(b,"BCryptCloseAlgorithmProvider"); destroy=(destroy_fn)GetProcAddress(b,"BCryptDestroyKey");
    if (!open || !hash || !import_key || !verify || !close || !destroy) return 0;
    if (open(&sha,BCRYPT_SHA256_ALGORITHM,NULL,0)>=0 && open(&rsa,BCRYPT_RSA_ALGORITHM,NULL,0)>=0 &&
        hash(sha,NULL,0,(PUCHAR)bytes,length,digest,32)>=0 &&
        import_key(rsa,NULL,BCRYPT_RSAPUBLIC_BLOB,&key,(PUCHAR)pg_public_key,sizeof(pg_public_key),0)>=0 &&
        verify(key,&padding,digest,32,(PUCHAR)signature,signature_length,BCRYPT_PAD_PKCS1)>=0) ok=1;
    if(key) destroy(key); if(sha) close(sha,0); if(rsa) close(rsa,0); return ok;
}
static const BYTE *pg_resource(unsigned id,DWORD *length) {
    HRSRC r=FindResourceW(NULL,MAKEINTRESOURCEW(id),RT_RCDATA); HGLOBAL h;
    if(!r || !(*length=SizeofResource(NULL,r)) || !(h=LoadResource(NULL,r))) return NULL;
    return (const BYTE*)LockResource(h);
}
static int pg_write(const wchar_t *path,const BYTE *data,DWORD size) {
    HANDLE h=CreateFileW(path,GENERIC_READ|GENERIC_WRITE,0,NULL,CREATE_NEW,FILE_FLAG_OPEN_REPARSE_POINT|FILE_FLAG_WRITE_THROUGH,NULL);
    DWORD written; BYTE buffer[4096]; DWORD n,offset=0; LARGE_INTEGER length;
    if(h!=INVALID_HANDLE_VALUE) {
        if(!WriteFile(h,data,size,&written,NULL) || written!=size || !FlushFileBuffers(h)) { CloseHandle(h); return 0; }
        /* Close only after durable write, then reacquire a non-write sharing pin.
         * Any competing write/delete handle makes reacquisition fail closed. */
        CloseHandle(h);
    } else if(GetLastError()!=ERROR_FILE_EXISTS && GetLastError()!=ERROR_ALREADY_EXISTS) return 0;
    h=pg_open(path,0); if(!h) return 0;
    if(!GetFileSizeEx(h,&length) || length.QuadPart!=size) { CloseHandle(h); return 0; }
    while(offset<size) {
        DWORD want=size-offset; if(want>sizeof(buffer)) want=sizeof(buffer);
        if(!ReadFile(h,buffer,want,&n,NULL) || n!=want || memcmp(buffer,data+offset,n)) { CloseHandle(h); return 0; }
        offset+=n;
    }
    return pg_hold(h);
}
static int pg_folder_paths(HANDLE token) {
    typedef HRESULT (WINAPI *folder_fn)(REFKNOWNFOLDERID,DWORD,HANDLE,PWSTR*);
    typedef void (WINAPI *free_fn)(LPVOID);
    HMODULE shell=pg_system_library(L"shell32.dll"),ole=pg_system_library(L"ole32.dll");
    folder_fn folder; free_fn release; PWSTR path=NULL; unsigned i;
    const GUID *ids[4]={&pg_programs,&pg_local,&pg_menu,&pg_startup};
    if(!shell || !ole) return 0; folder=(folder_fn)GetProcAddress(shell,"SHGetKnownFolderPath"); release=(free_fn)GetProcAddress(ole,"CoTaskMemFree");
    if(!folder || !release) return 0;
    for(i=0;i<4;i++) {
        if(FAILED(folder(ids[i],0x4000u,i?token:NULL,&path)) || !path) {if(path) release(path);return 0;}
        if(!pg_local_path(path)) {release(path);return 0;}
        if(wcscpy_s(g.root_paths[i],32768,path)) {release(path);return 0;} release(path); path=NULL;
    }
    return 1;
}
static int pg_check_roots(void) {
    unsigned i; HANDLE current; int ok;
    for(i=0;i<4;i++) {
        current=pg_open(g.root_paths[i],1); ok=pg_same_file(current,g.roots[i]); if(current) CloseHandle(current);
        if(!ok) return 0;
    }
    return 1;
}
static int pg_capture_roots(void) {
    unsigned i; HANDLE access; BY_HANDLE_FILE_INFORMATION granted,pinned;
    if(!pg_folder_paths(NULL)) return 0;
    for(i=0;i<4;i++) {
        /* Elevation is for Program Files, not for turning a redirected user
         * KnownFolder into a write capability the original user never held. */
        if(i) {
            access=CreateFileW(g.root_paths[i],FILE_ADD_FILE|FILE_ADD_SUBDIRECTORY|FILE_READ_ATTRIBUTES,
                FILE_SHARE_READ|FILE_SHARE_WRITE|FILE_SHARE_DELETE,NULL,OPEN_EXISTING,FILE_FLAG_OPEN_REPARSE_POINT|FILE_FLAG_BACKUP_SEMANTICS,NULL);
            if(access==INVALID_HANDLE_VALUE) return 0;
            if(!GetFileInformationByHandle(access,&granted) || (granted.dwFileAttributes&FILE_ATTRIBUTE_REPARSE_POINT) || !(granted.dwFileAttributes&FILE_ATTRIBUTE_DIRECTORY)) {CloseHandle(access);return 0;}
            CloseHandle(access);
        }
        g.roots[i]=pg_open(g.root_paths[i],1);if(!pg_hold(g.roots[i])) return 0;
        if(i && (!GetFileInformationByHandle(g.roots[i],&pinned) || granted.dwVolumeSerialNumber!=pinned.dwVolumeSerialNumber ||
            granted.nFileIndexHigh!=pinned.nFileIndexHigh || granted.nFileIndexLow!=pinned.nFileIndexLow ||
            granted.ftCreationTime.dwHighDateTime!=pinned.ftCreationTime.dwHighDateTime || granted.ftCreationTime.dwLowDateTime!=pinned.ftCreationTime.dwLowDateTime)) return 0;
    }
    return 1;
}
static int pg_roots(void) {
    return pg_check_roots() && swprintf_s(g.runtime,32768,L"%s\\ProGo",g.root_paths[0])>0 &&
        swprintf_s(g.data,32768,L"%s\\ProGo",g.root_paths[1])>0 &&
        swprintf_s(g.payload,32768,L"%s\\backups\\personal-broker\\payload",g.runtime)>0 &&
        pg_pin_tree(g.payload,1) && pg_pin_tree(g.data,1);
}
static int pg_extract(void) {
    DWORD size,sigsize,pos=12,count,i; const BYTE *bytes=pg_resource(101,&size),*sig=pg_resource(102,&sigsize);
    wchar_t destination[32768],parent[32768],name[512]; unsigned j,n; DWORD length;
    if(!bytes || !sig || size<12 || size>PG_MAX_ARCHIVE || memcmp(bytes,"PGPK0001",8) || !pg_verify(bytes,size,sig,sigsize)) return 0;
    memcpy(&count,bytes+8,4); if(count!=pg_payload_count) return 0;
    /* Validate the WHOLE authenticated frame before creating any payload leaf. */
    for(i=0;i<count;++i) {
        if(pos>size-6) return 0; n=bytes[pos]|((unsigned)bytes[pos+1]<<8); memcpy(&length,bytes+pos+2,4); pos+=6;
        if(n==0 || n>=512 || n>size-pos || length>size-pos-n) return 0;
        for(j=0;j<n;++j) { if(bytes[pos+j]<32 || bytes[pos+j]>126) return 0; name[j]=(wchar_t)bytes[pos+j]; } name[n]=0;
        if(wcscmp(name,pg_payload_names[i])) return 0; pos+=n+length;
    }
    if(pos!=size || !pg_roots()) return 0;
    pos=12;
    for(i=0;i<count;++i) {
        if(pos>size-6) return 0; n=bytes[pos]|((unsigned)bytes[pos+1]<<8); memcpy(&length,bytes+pos+2,4); pos+=6;
        if(n==0 || n>=512 || n>size-pos || length>size-pos-n) return 0;
        for(j=0;j<n;++j) { if(bytes[pos+j]<32 || bytes[pos+j]>126) return 0; name[j]=(wchar_t)bytes[pos+j]; } name[n]=0;
        if(wcscmp(name,pg_payload_names[i])) return 0; pos+=n;
        if(swprintf_s(destination,32768,L"%s\\%s",g.payload,name)<0) return 0;
        for(j=0;destination[j];++j) if(destination[j]==L'/') destination[j]=L'\\';
        if(wcscpy_s(parent,32768,destination)) return 0; *wcsrchr(parent,L'\\')=0;
        if(!pg_pin_tree(parent,1) || !pg_write(destination,bytes+pos,length)) return 0; pos+=length;
    }
    return pos==size;
}
static int pg_retain_image(void) {
    LARGE_INTEGER length; BYTE *bytes; DWORD n; wchar_t destination[32768]; int ok=0;
    if(!GetFileSizeEx(g.image,&length) || length.QuadPart<1 || length.QuadPart>PG_MAX_ARCHIVE) return 0;
    bytes=(BYTE*)HeapAlloc(GetProcessHeap(),0,(SIZE_T)length.QuadPart); if(!bytes) return 0;
    SetFilePointer(g.image,0,NULL,FILE_BEGIN);
    if(ReadFile(g.image,bytes,(DWORD)length.QuadPart,&n,NULL) && n==length.QuadPart &&
        swprintf_s(destination,32768,L"%s\\backups\\personal-broker\\ProGo.Setup.exe",g.runtime)>0)
        ok=pg_write(destination,bytes,n);
    HeapFree(GetProcessHeap(),0,bytes); return ok;
}
static HANDLE pg_pipe(wchar_t *name) {
    BYTE random[16]; unsigned i; wchar_t hex[33];
    if(!pg_random(random,16)) return NULL;
    for(i=0;i<16;++i) swprintf_s(hex+i*2,3,L"%02x",random[i]);
    if(swprintf_s(name,128,L"\\\\.\\pipe\\ProGo-personal-%s",hex)<0) return NULL;
    return CreateNamedPipeW(name,PIPE_ACCESS_DUPLEX|FILE_FLAG_FIRST_PIPE_INSTANCE|FILE_FLAG_OVERLAPPED,
        PIPE_TYPE_MESSAGE|PIPE_READMODE_MESSAGE|PIPE_WAIT|PIPE_REJECT_REMOTE_CLIENTS,1,64,64,0,NULL);
}
typedef struct pg_async { OVERLAPPED overlap; BYTE bytes[8]; } pg_async;
static pg_async *pg_async_new(void) {
    pg_async *op=(pg_async*)HeapAlloc(GetProcessHeap(),HEAP_ZERO_MEMORY,sizeof(pg_async));
    if(!op) return NULL;
    op->overlap.hEvent=CreateEventW(NULL,TRUE,FALSE,NULL);
    if(!op->overlap.hEvent) { HeapFree(GetProcessHeap(),0,op); return NULL; }
    return op;
}
static void pg_async_done(pg_async *op) { CloseHandle(op->overlap.hEvent); HeapFree(GetProcessHeap(),0,op); }
static void pg_async_cancel(HANDLE pipe,pg_async *op) {
    CancelIoEx(pipe,&op->overlap);
    if(WaitForSingleObject(op->overlap.hEvent,5000)==WAIT_OBJECT_0) pg_async_done(op);
    /* If the OS has not completed cancellation, keep event/OVERLAPPED/buffer
     * owned by this process until exit. Never return a pending stack buffer or
     * wait indefinitely, and never reuse it as a new request. */
}
static int pg_connect(HANDLE pipe,HANDLE process) {
    pg_async *op=pg_async_new(); HANDLE waits[2]; DWORD n; BOOL connected; int result=0;
    if(!op) return 0;
    connected=ConnectNamedPipe(pipe,&op->overlap);
    if(connected || GetLastError()==ERROR_PIPE_CONNECTED) result=1;
    else if(GetLastError()==ERROR_IO_PENDING) {
        waits[0]=op->overlap.hEvent; waits[1]=process;
        if(WaitForMultipleObjects(2,waits,FALSE,30000)==WAIT_OBJECT_0) result=GetOverlappedResult(pipe,&op->overlap,&n,FALSE)!=FALSE;
        else { pg_async_cancel(pipe,op); return 0; }
    }
    pg_async_done(op); return result;
}
static int pg_io(HANDLE pipe,void *buffer,DWORD length,int write) {
    pg_async *op=pg_async_new(); DWORD n=0; BOOL ok; int result=0;
    if(!op) return 0;
    if(length>sizeof(op->bytes)) {pg_async_done(op);return 0;}
    if(write) memcpy(op->bytes,buffer,length);
    ok=write?WriteFile(pipe,op->bytes,length,NULL,&op->overlap):ReadFile(pipe,op->bytes,length,NULL,&op->overlap);
    if(ok) result=GetOverlappedResult(pipe,&op->overlap,&n,FALSE) && n==length;
    else if(GetLastError()==ERROR_IO_PENDING) {
        ULONGLONG now=GetTickCount64(); DWORD remaining=now>=g.deadline?0:(DWORD)(g.deadline-now);
        if(WaitForSingleObject(op->overlap.hEvent,remaining)==WAIT_OBJECT_0) result=GetOverlappedResult(pipe,&op->overlap,&n,FALSE) && n==length;
        else { pg_async_cancel(pipe,op); return 0; }
    }
    if(result && !write) memcpy(buffer,op->bytes,length);
    pg_async_done(op); return result;
}
static int pg_send_roots(HANDLE pipe) {
    unsigned i;
    for(i=0;i<4;i++) {ULONGLONG handle=(ULONGLONG)(ULONG_PTR)g.roots[i]; if(!pg_io(pipe,&handle,sizeof(handle),1)) return 0;}
    return 1;
}
static int pg_receive_roots(HANDLE pipe) {
    unsigned i; ULONGLONG handle;
    for(i=0;i<4;i++) {
        if(!pg_io(pipe,&handle,sizeof(handle),0) || !handle || handle==(ULONGLONG)-1 ||
            !DuplicateHandle(g.original,(HANDLE)(ULONG_PTR)handle,GetCurrentProcess(),&g.roots[i],0,FALSE,DUPLICATE_SAME_ACCESS) ||
            !pg_hold(g.roots[i])) return 0;
    }
    return pg_folder_paths(g.original_token) && pg_check_roots();
}
static int pg_start_host(const wchar_t *script,const wchar_t *working,const wchar_t *scratch,const wchar_t *arguments,PROCESS_INFORMATION *pi) {
    wchar_t engine[32768],command[32768],*environment; STARTUPINFOW si; HANDLE engine_pin;
    JOBOBJECT_EXTENDED_LIMIT_INFORMATION limits;
    PG_STAGE(PG_D_COMMAND);
    if(swprintf_s(engine,32768,L"%s\\WindowsPowerShell\\v1.0\\powershell.exe",g.system)<0 ||
        swprintf_s(command,32768,L"\"%s\" -NoLogo -NoProfile -NonInteractive -File \"%s\" %s",engine,script,arguments)<0) return 0;
    PG_STAGE(PG_D_ENGINE);
    engine_pin=pg_open(engine,0); if(!pg_hold(engine_pin)) return 0;
    PG_STAGE(PG_D_ENV_ALLOC);
    environment=(wchar_t*)HeapAlloc(GetProcessHeap(),HEAP_ZERO_MEMORY,131072*sizeof(wchar_t)); if(!environment) return 0;
    PG_STAGE(PG_D_ENV);
    if(!pg_environment(environment,131072,g.windows,g.system,scratch)) { HeapFree(GetProcessHeap(),0,environment); return 0; }
    ZeroMemory(&si,sizeof(si)); si.cb=sizeof(si); ZeroMemory(pi,sizeof(*pi));
    PG_STAGE(PG_D_JOB_CREATE);
    g.job=CreateJobObjectW(NULL,NULL);
    if(!g.job) { PG_ERROR(); HeapFree(GetProcessHeap(),0,environment); return 0; }
    ZeroMemory(&limits,sizeof(limits));
    limits.BasicLimitInformation.LimitFlags=JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;
    PG_STAGE(PG_D_JOB_LIMIT);
    if(!SetInformationJobObject(g.job,JobObjectExtendedLimitInformation,&limits,sizeof(limits))) { PG_ERROR(); HeapFree(GetProcessHeap(),0,environment); return 0; }
    /* No ShellExecute/elevation, inherited handles, script text evaluation or
     * ExecutionPolicy override in the privileged managed-host launch. */
    PG_STAGE(PG_D_CREATE);
    if(!CreateProcessW(engine,command,NULL,NULL,FALSE,CREATE_SUSPENDED|CREATE_UNICODE_ENVIRONMENT|CREATE_NO_WINDOW,environment,working,&si,pi)) { PG_ERROR(); HeapFree(GetProcessHeap(),0,environment); return 0; }
    PG_FLAG(created);
    PG_STAGE(PG_D_ASSIGN);
    if(!AssignProcessToJobObject(g.job,pi->hProcess)) {
        PG_ERROR();
        TerminateProcess(pi->hProcess,3); WaitForSingleObject(pi->hProcess,5000); CloseHandle(pi->hThread); CloseHandle(pi->hProcess);
        HeapFree(GetProcessHeap(),0,environment); return 0;
    }
    PG_FLAG(assigned);
    PG_STAGE(PG_D_RESUME);
    if(ResumeThread(pi->hThread)==(DWORD)-1) {
        PG_ERROR();
        TerminateProcess(pi->hProcess,3); WaitForSingleObject(pi->hProcess,5000); CloseHandle(pi->hThread); CloseHandle(pi->hProcess);
        HeapFree(GetProcessHeap(),0,environment); return 0;
    }
    PG_FLAG(resumed);
    HeapFree(GetProcessHeap(),0,environment); CloseHandle(pi->hThread); return 1;
}
static int pg_child(void) {
    wchar_t pipe_name[128],script[32768],scratch[32768],arguments[256];
    PROCESS_INFORMATION pi; DWORD id,frame[2],result=1,code=1; int status=3;
    if(GetTickCount64()>=g.deadline) return 3;
    g.pipe=pg_pipe(pipe_name); if(!g.pipe || g.pipe==INVALID_HANDLE_VALUE) return 3;
    if(swprintf_s(script,32768,L"%s\\PersonalBroker.ps1",g.payload)<0 ||
        swprintf_s(scratch,32768,L"%s\\temp",g.payload)<0 || !pg_pin_tree(scratch,1) ||
        swprintf_s(arguments,256,L"-BrokerPipe \"%s\"",pipe_name)<0 || !pg_start_host(script,g.system,scratch,arguments,&pi)) return 3;
    g.client=pi.hProcess;
    if(pg_connect(g.pipe,g.client) && GetNamedPipeClientProcessId(g.pipe,&id) && id==pi.dwProcessId &&
        pg_claim(&g.exchange,g.exchange.operation)) {
        frame[0]=g.exchange.operation; frame[1]=g.original_pid;
        ULONGLONG image_reference=(ULONGLONG)(ULONG_PTR)g.image;
        if(pg_io(g.pipe,frame,sizeof(frame),1) && pg_send_roots(g.pipe) && pg_io(g.pipe,&image_reference,sizeof(image_reference),1)) {
            while(pg_io(g.pipe,&result,sizeof(result),0)) {
                if(result==2 && pg_heartbeat(&g.exchange,WaitForSingleObject(g.original,0)==WAIT_TIMEOUT,GetTickCount64()<g.deadline) && pg_check_roots()) {
                    if(!pg_io(g.pipe,&result,sizeof(result),1)) break;
                    continue;
                }
                if(result<=1 && WaitForSingleObject(g.client,5000)==WAIT_OBJECT_0 && GetExitCodeProcess(g.client,&code) && code==result && pg_settle(&g.exchange,result)) status=(int)result;
                break;
            }
        }
    }
    /* Revoke admission, allow cooperative catch/finally settlement, then bound
     * the exact job-owned tree. Forced termination means RECOVERY_REQUIRED,
     * never rollback-success. Durable pending/terminal/stage evidence is retained. */
    CloseHandle(g.pipe); g.pipe=NULL;
    if(WaitForSingleObject(g.client,5000)!=WAIT_OBJECT_0) { TerminateJobObject(g.job,3); WaitForSingleObject(g.client,5000); status=3; }
    { JOBOBJECT_BASIC_ACCOUNTING_INFORMATION accounting;
      if(!QueryInformationJobObject(g.job,JobObjectBasicAccountingInformation,&accounting,sizeof(accounting),NULL) || accounting.ActiveProcesses!=0) {
          TerminateJobObject(g.job,3); status=3;
      }
    }
    return status;
}
#ifdef PROGO_BROKER_NATIVE_FIXTURE
/* Fixture-only: pin every nonreparse component, including the volume root,
 * then retain an exact directory handle. Neither spelling nor telemetry admits. */
static int pg_fixture_pin_directory(wchar_t *path,HANDLE *exact) {
    wchar_t drive[4],saved; HANDLE h; size_t i,n=wcslen(path);
    if(!pg_local_path(path)) return 0;
    memcpy(drive,path,3*sizeof(wchar_t)); drive[3]=0;
    if(!pg_hold(pg_open(drive,1))) return 0;
    /* Inspect each spelling independently: case-folded cache hits cannot prove
     * nonreparse ancestry on case-sensitive directories. No directory cache. */
    for(i=3;i<=n;++i) if(path[i]==L'\\' || path[i]==0) {
        saved=path[i]; path[i]=0;
        h=pg_open(path,1);
        if(!pg_hold(h)) { path[i]=saved; return 0; }
        path[i]=saved;
    }
    h=pg_open(path,1); if(!pg_hold(h)) return 0;
    *exact=h; return 1;
}
static int pg_fixture_temp_identity(wchar_t *root,wchar_t *temporary) {
    wchar_t parent[32768],temp[32768],*leaf; size_t n;
    HANDLE fixture_pin=NULL,parent_pin=NULL,temp_pin=NULL;
    pg_diag.identity_result=1; /* chain/exact-directory pin refused */
    if(wcscpy_s(parent,32768,root) || wcscpy_s(temp,32768,temporary)) return 0;
    leaf=wcsrchr(parent,L'\\'); if(!leaf || !pg_fixture_leaf(leaf+1)) return 0;
    if(leaf==parent+2) leaf[1]=0; else *leaf=0;
    n=wcslen(temp); while(n>3 && temp[n-1]==L'\\') temp[--n]=0;
    if(!pg_fixture_pin_directory(root,&fixture_pin) ||
        !pg_fixture_pin_directory(parent,&parent_pin) ||
        !pg_fixture_pin_directory(temp,&temp_pin)) return 0;
    pg_diag.identity_result=2; /* identity mismatch or failed identity API */
    SetLastError(ERROR_SUCCESS);
    if(!pg_same_file(parent_pin,temp_pin)) return 0;
    pg_diag.identity_result=3; /* exact pinned directory identity equal */
    return 1;
}
static int pg_fixture_host(void) {
    wchar_t root[32768],script[32768],scratch[32768],temporary[32768],*leaf;
    wchar_t long_root[32768],long_temp[32768]; DWORD count,wait; PROCESS_INFORMATION pi; DWORD code=1;
    JOBOBJECT_BASIC_ACCOUNTING_INFORMATION accounting;
    PG_STAGE(PG_D_FIXTURE_ROOT);
    if(wcscpy_s(root,32768,g.self)) return 1; *wcsrchr(root,L'\\')=0;
    leaf=wcsrchr(root,L'\\'); if(!leaf || !pg_fixture_leaf(leaf+1)) return 1;
    PG_STAGE(PG_D_TEMP);
    count=GetTempPathW(32768,temporary); if(!count || count>=32768) { PG_ERROR(); return 1; }
    *leaf=0;
    pg_diag.temp_match=!_wcsnicmp(root,temporary,wcslen(root)) && wcslen(temporary)==wcslen(root)+1;
    /* Observation only: long-name equality cannot authorize this launch. */
    count=GetLongPathNameW(root,long_root,32768);
    if(count && count<32768) {
        count=GetLongPathNameW(temporary,long_temp,32768);
        if(count && count<32768) {
            size_t n=wcslen(long_temp); if(n && long_temp[n-1]==L'\\') long_temp[n-1]=0;
            pg_diag.long_known=1; pg_diag.long_match=!_wcsicmp(long_root,long_temp);
        }
    }
    PG_STAGE(PG_D_TEMP_IDENTITY);
    *leaf=L'\\';
    if(!pg_fixture_temp_identity(root,temporary)) return 1;
    PG_STAGE(PG_D_SCRATCH);
    if(swprintf_s(script,32768,L"%s\\host.ps1",root)<0 || swprintf_s(scratch,32768,L"%s\\scratch",root)<0 || !pg_pin_tree(scratch,1)) return 1;
    if(!pg_start_host(script,root,scratch,L"",&pi)) return 1;
    g.client=pi.hProcess;
    PG_STAGE(PG_D_WAIT);
    wait=WaitForSingleObject(g.client,60000);
    pg_diag.wait_result=wait;
    if(wait!=WAIT_OBJECT_0) {
        if(wait==WAIT_FAILED) PG_ERROR();
        TerminateJobObject(g.job,3); WaitForSingleObject(g.client,5000); return 3;
    }
    PG_STAGE(PG_D_EXIT);
    if(!GetExitCodeProcess(g.client,&code)) { PG_ERROR(); TerminateJobObject(g.job,3); return 3; }
    pg_diag.exit_known=1; pg_diag.host_exit=code;
    PG_STAGE(PG_D_ACCOUNT);
    if(!QueryInformationJobObject(g.job,JobObjectBasicAccountingInformation,&accounting,sizeof(accounting),NULL)) {
        PG_ERROR(); TerminateJobObject(g.job,3); return 3;
    }
    pg_diag.account_ok=1; pg_diag.active=accounting.ActiveProcesses;
    if(accounting.ActiveProcesses!=0) { TerminateJobObject(g.job,3); return 3; }
    PG_STAGE(PG_D_DONE);
    return (int)code;
}
#endif
static int pg_worker(const wchar_t *name) {
    DWORD server,op=0,result=1; HANDLE pipe;
    pipe=CreateFileW(name,GENERIC_READ|GENERIC_WRITE,0,NULL,OPEN_EXISTING,FILE_FLAG_OVERLAPPED|SECURITY_SQOS_PRESENT|SECURITY_IDENTIFICATION,NULL);
    if(pipe==INVALID_HANDLE_VALUE) return 1;
    if(GetNamedPipeServerProcessId(pipe,&server)) {
        g.original=OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION|PROCESS_DUP_HANDLE|SYNCHRONIZE,FALSE,server); g.original_pid=server;
        if(g.original && pg_image(g.original) && pg_token(g.original,TOKEN_QUERY|TOKEN_IMPERSONATE|TOKEN_DUPLICATE,&g.original_token) && pg_linked(g.self_token,g.original_token) &&
            WaitForSingleObject(g.original,0)==WAIT_TIMEOUT && pg_io(pipe,&op,sizeof(op),0) && pg_receive_roots(pipe) &&
            pg_admit(&g.exchange,op,1,1,1,1,1,1) && pg_extract() && pg_retain_image()) result=(DWORD)pg_child();
    }
    pg_io(pipe,&result,sizeof(result),1); CloseHandle(pipe); return (int)result;
}
static int pg_parent(unsigned op) {
    typedef BOOL (WINAPI *shell_fn)(SHELLEXECUTEINFOW*);
    wchar_t name[128],args[256]; SHELLEXECUTEINFOW s; TOKEN_ELEVATION_TYPE type; DWORD id,result=1;
    HANDLE worker_token=NULL; HMODULE shell=pg_system_library(L"shell32.dll"); shell_fn execute;
    if(!shell || !pg_info(g.self_token,TokenElevationType,&type,sizeof(type)) || type!=TokenElevationTypeLimited) return 1;
    if(!pg_capture_roots()) return 1;
    execute=(shell_fn)GetProcAddress(shell,"ShellExecuteExW"); if(!execute) return 1;
    g.pipe=pg_pipe(name); if(!g.pipe || g.pipe==INVALID_HANDLE_VALUE || swprintf_s(args,256,L"--worker %s",name)<0) return 1;
    ZeroMemory(&s,sizeof(s)); s.cbSize=sizeof(s); s.fMask=SEE_MASK_NOCLOSEPROCESS|SEE_MASK_NOASYNC|SEE_MASK_FLAG_NO_UI;
    s.lpVerb=L"runas"; s.lpFile=g.self; s.lpParameters=args; s.lpDirectory=g.system; s.nShow=SW_HIDE;
    if(!execute(&s)) return GetLastError()==ERROR_CANCELLED?2:1;
    g.deadline=GetTickCount64()+PG_LIFETIME_MS;
    g.client=s.hProcess;
    if(pg_connect(g.pipe,g.client) && GetNamedPipeClientProcessId(g.pipe,&id) && id==GetProcessId(g.client) &&
        pg_image(g.client) && pg_token(g.client,TOKEN_QUERY,&worker_token) && pg_linked(worker_token,g.self_token) &&
        pg_io(g.pipe,&op,sizeof(op),1) && pg_send_roots(g.pipe) && pg_io(g.pipe,&result,sizeof(result),0) && (result<=1 || result==3)) {
        DWORD code=1;
        if(WaitForSingleObject(g.client,5000)!=WAIT_OBJECT_0 || !GetExitCodeProcess(g.client,&code) || code!=result) result=1;
    } else result=1;
    if(worker_token) CloseHandle(worker_token); return (int)result;
}
static int pg_main(HINSTANCE instance,HINSTANCE previous,PWSTR cmd,int show) {
    typedef LPWSTR* (WINAPI *argv_fn)(LPCWSTR,int*);
    HMODULE shell; argv_fn split; wchar_t **argv; int argc=0,result=1; unsigned i;
    (void)instance; (void)previous; (void)cmd; (void)show;
    PG_STAGE(PG_D_SYSTEM);
    if(!GetSystemDirectoryW(g.system,32768)) { PG_ERROR(); return 1; }
    PG_STAGE(PG_D_WINDOWS);
    if(!GetSystemWindowsDirectoryW(g.windows,32768)) { PG_ERROR(); return 1; }
    PG_STAGE(PG_D_DLL_POLICY);
    if(!SetDefaultDllDirectories(LOAD_LIBRARY_SEARCH_SYSTEM32)) { PG_ERROR(); return 1; }
    PG_STAGE(PG_D_MODULE);
    if(!GetModuleFileNameW(NULL,g.self,32768)) { PG_ERROR(); return 1; }
    PG_STAGE(PG_D_LOCAL_IMAGE);
    if(!pg_local_path(g.self)) return 1;
    PG_STAGE(PG_D_IMAGE);
    g.image=pg_open(g.self,0); if(!pg_hold(g.image)) return 1;
    PG_STAGE(PG_D_TOKEN);
    if(!pg_token(GetCurrentProcess(),TOKEN_QUERY,&g.self_token)) { PG_ERROR(); return 1; }
    PG_STAGE(PG_D_SHELL);
    shell=pg_system_library(L"shell32.dll"); if(!shell) { PG_ERROR(); return 1; }
    PG_STAGE(PG_D_SPLIT);
    split=(argv_fn)GetProcAddress(shell,"CommandLineToArgvW"); if(!split) { PG_ERROR(); return 1; }
    PG_STAGE(PG_D_ARGV);
    argv=split(GetCommandLineW(),&argc); if(!argv) { PG_ERROR(); return 1; }
    g.deadline=GetTickCount64()+PG_LIFETIME_MS;
#ifdef PROGO_BROKER_NATIVE_FIXTURE
    if(argc==2 && !wcscmp(argv[1],L"--fixture-host")) result=pg_fixture_host();
    else
#endif
    if(argc==1 || (argc==2 && !wcscmp(argv[1],L"--install"))) result=pg_parent(PG_INSTALL);
    else if(argc==2 && !wcscmp(argv[1],L"--uninstall")) result=pg_parent(PG_UNINSTALL);
    else if(argc==3 && !wcscmp(argv[1],L"--worker") && pg_route(argv[2])) result=pg_worker(argv[2]);
    LocalFree(argv);
    if(g.client) CloseHandle(g.client); if(g.pipe && g.pipe!=INVALID_HANDLE_VALUE) CloseHandle(g.pipe);
    if(g.job) CloseHandle(g.job);
    if(g.original_token) CloseHandle(g.original_token); if(g.original) CloseHandle(g.original); if(g.self_token) CloseHandle(g.self_token);
    for(i=g.pin_count;i>0;--i) CloseHandle(g.pins[i-1]);
    for(i=g.directory_count;i>0;--i) HeapFree(GetProcessHeap(),0,g.directories[i-1]); return result;
}

int WINAPI wWinMain(HINSTANCE instance,HINSTANCE previous,PWSTR cmd,int show) {
    int result=pg_main(instance,previous,cmd,show);
#ifdef PROGO_BROKER_NATIVE_FIXTURE
    pg_diag_write();
#endif
    return result;
}
