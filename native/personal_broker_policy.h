/* Shared by the real native launcher and the portable refusal fixtures.
 * No Windows/CLR initialization, environment lookup or privilege acquisition here. */
#ifndef PROGO_PERSONAL_BROKER_POLICY_H
#define PROGO_PERSONAL_BROKER_POLICY_H
#include <stddef.h>
#include <wchar.h>
#define PG_INSTALL 1u
#define PG_UNINSTALL 2u
#define PG_MAX_ARCHIVE (64u * 1024u * 1024u)
#define PG_LIFETIME_MS 300000u
typedef struct pg_exchange {
    unsigned operation, phase, claimed, settled;
} pg_exchange;
static int pg_fixture_leaf(const wchar_t *leaf) {
    const wchar_t prefix[]=L"ProGo-native-broker-fixture-";
    size_t i, length=(sizeof(prefix)/sizeof(prefix[0]))-1;
    if(!leaf || wcslen(leaf)!=length+32 || wcsncmp(leaf,prefix,length)) return 0;
    for(i=length;i<length+32;i++)
        if(!((leaf[i]>=L'0' && leaf[i]<=L'9') || (leaf[i]>=L'a' && leaf[i]<=L'f'))) return 0;
    return 1;
}
static int pg_operation(unsigned op) { return op == PG_INSTALL || op == PG_UNINSTALL; }
static int pg_route(const wchar_t *name) {
    size_t i;
    if(!name || wcslen(name)!=56 || wcsncmp(name,L"\\\\.\\pipe\\ProGo-personal-",24)) return 0;
    for(i=24;i<56;i++) if(!((name[i]>=L'0' && name[i]<=L'9') || (name[i]>=L'a' && name[i]<=L'f'))) return 0;
    return 1;
}
static int pg_admit(pg_exchange *x, unsigned op, int native_origin, int same_user,
                    int linked_token, int same_session, int original_alive, int fresh) {
    if (!x || x->phase || !pg_operation(op) || !native_origin || !same_user ||
        !linked_token || !same_session || !original_alive || !fresh) return 0;
    x->operation = op; x->phase = 1; return 1;
}
static int pg_claim(pg_exchange *x, unsigned op) {
    if (!x || x->phase != 1 || x->claimed || x->operation != op) return 0;
    x->claimed = 1; x->phase = 2; return 1;
}
static int pg_settle(pg_exchange *x, unsigned result) {
    if (!x || x->phase != 2 || x->settled || result > 1) return 0;
    x->settled = 1; x->phase = 3; return 1;
}
static int pg_heartbeat(const pg_exchange *x,int original_alive,int within_lifetime) {
    return x && x->phase==2 && !x->settled && original_alive && within_lifetime;
}
/* Build an allowlist from trusted Win32 directories BEFORE CreateProcess.
 * No inherited environment is copied, including CLR/profiler/PS startup knobs.
 * The double-NUL block is passed with CREATE_UNICODE_ENVIRONMENT. */
static int pg_environment(wchar_t *out, size_t cap, const wchar_t *windows,
                          const wchar_t *system, const wchar_t *scratch) {
    const wchar_t *keys[] = { L"PATH=", L"PSModulePath=", L"SystemRoot=", L"TEMP=", L"TMP=", L"WINDIR=" };
    const wchar_t *values[6]; size_t i, j, n = 0;
    wchar_t modules[32768]; size_t m = 0;
    const wchar_t *suffix = L"\\WindowsPowerShell\\v1.0\\Modules";
    if (!out || !windows || !system || !scratch || windows[0] == 0 || system[0] == 0 || scratch[0] == 0) return 0;
    for (j = 0; system[j]; ++j) { if (m + 1 >= 32768) return 0; modules[m++] = system[j]; }
    for (j = 0; suffix[j]; ++j) { if (m + 1 >= 32768) return 0; modules[m++] = suffix[j]; }
    modules[m] = 0;
    values[0] = system; values[1] = modules; values[2] = windows;
    values[3] = scratch; values[4] = scratch; values[5] = windows;
    for (i = 0; i < 6; ++i) {
        for (j = 0; keys[i][j]; ++j) { if (n + 1 >= cap) return 0; out[n++] = keys[i][j]; }
        for (j = 0; values[i][j]; ++j) {
            if (values[i][j] == L'\r' || values[i][j] == L'\n' || n + 1 >= cap) return 0;
            out[n++] = values[i][j];
        }
        if (n + 1 >= cap) return 0;
        out[n++] = 0;
    }
    out[n] = 0; return 1;
}
#endif
