"""Canonical native bootstrap identity; only sealed RCDATA may vary after link.

This is a build/review tool, never an elevated runtime dependency. Loader-critical
headers, code/data, import tables, relocations and the embedded manifest are bound.
Resource/relocation placement and PE checksums may change during UpdateResource.
"""
import hashlib
import pathlib
import struct
import sys


def inspect(data):
    def take(offset, size):
        if offset < 0 or size < 0 or offset + size > len(data):
            raise ValueError('Bootstrap image bounds differ.')
        return data[offset:offset + size]

    def u16(o): return struct.unpack('<H', take(o, 2))[0]
    def u32(o): return struct.unpack('<I', take(o, 4))[0]
    if take(0, 2) != b'MZ': raise ValueError('Missing DOS image.')
    pe = u32(60)
    if take(pe, 4) != b'PE\0\0' or u16(pe + 4) != 0x8664:
        raise ValueError('Bootstrap must be native x64.')
    count, opt_size = u16(pe + 6), u16(pe + 20)
    opt, sections = pe + 24, []
    if not 1 <= count <= 16 or opt_size != 240 or u16(opt) != 0x20b:
        raise ValueError('Bootstrap optional header differs.')
    if u32(opt + 108) != 16 or u32(opt + 112 + 14 * 8):
        raise ValueError('Managed bootstrap is forbidden.')
    if u32(opt + 112 + 9 * 8) or u32(opt + 112 + 4 * 8):
        raise ValueError('Unexpected TLS or certificate directory.')
    for i in range(count):
        off = opt + opt_size + i * 40
        name = take(off, 8)
        vs, va, size, raw, flags = u32(off+8), u32(off+12), u32(off+16), u32(off+20), u32(off+36)
        if any(s[0] == name for s in sections): raise ValueError('Duplicate section.')
        take(raw, size)
        if flags & 0x20000000 and name != b'.text\0\0\0':
            raise ValueError('Unexpected executable section.')
        if name in (b'.rsrc\0\0\0', b'.reloc\0\0') and flags & 0xa0000000:
            raise ValueError('Mutable executable resource/relocation section.')
        for _, other_vs, other_va, other_size, other_raw, _ in sections:
            if size and other_size and max(raw, other_raw) < min(raw+size, other_raw+other_size):
                raise ValueError('Overlapping raw sections.')
            if max(va, other_va) < min(va+max(vs,size), other_va+max(other_vs,other_size)):
                raise ValueError('Overlapping mapped sections.')
        sections.append((name, vs, va, size, raw, flags))

    def rva(address, size):
        candidates = [s for s in sections if s[2] <= address and address+size <= s[2]+s[3]]
        if len(candidates) != 1: raise ValueError('Ambiguous RVA.')
        return candidates[0][4] + address - candidates[0][2]

    # UpdateResource may move .reloc, but must never redirect loader fixups
    # into excluded resources. Bind the directory length and hashed section.
    relocations = [s for s in sections if s[0] == b'.reloc\0\0']
    reloc_rva, reloc_size = u32(opt+152), u32(opt+156)
    if reloc_rva or reloc_size or relocations:
        if (len(relocations) != 1 or not reloc_size or
                reloc_rva != relocations[0][2] or
                reloc_size > min(relocations[0][1], relocations[0][3])):
            raise ValueError('Relocation directory must belong to bounded .reloc.')
        rva(reloc_rva, reloc_size)

    imports = u32(opt + 120)
    if u32(opt + 216): raise ValueError('Delay-loaded bootstrap dependencies are forbidden.')
    if imports:
        for i in range(64):
            descriptor = rva(imports + i * 20, 20)
            if take(descriptor, 20) == bytes(20): break
            name = rva(u32(descriptor + 12), 1)
            end = data.find(b'\0', name, name + 128)
            if end < 0 or take(name, end - name).lower() != b'kernel32.dll':
                raise ValueError('Bootstrap imports a non-KnownDLL dependency.')
        else: raise ValueError('Bootstrap import inventory exceeds bounds.')

    resources_rva, resources_size = u32(opt+128), u32(opt+132)
    resource_root = rva(resources_rva, resources_size)
    rsrc = [s for s in sections if s[0] == b'.rsrc\0\0\0']
    if len(rsrc) != 1 or rsrc[0][2] != resources_rva:
        raise ValueError('Resource root differs.')

    def resource(kind, name=1):
        offset = resource_root
        for depth, wanted in enumerate((kind, name, None)):
            if offset + 16 > resource_root + resources_size: raise ValueError('Truncated resource directory.')
            amount = u16(offset+12) + u16(offset+14)
            if amount > 128 or offset+16+amount*8 > resource_root+resources_size: raise ValueError('Resource directory exceeds bounds.')
            entries = [(u32(offset+16+i*8), u32(offset+20+i*8)) for i in range(amount)]
            matches = [entry for entry in entries if (wanted is None or entry[0] == wanted)]
            if len(matches) != 1: raise ValueError('Resource is missing or ambiguous.')
            target = matches[0][1]
            if bool(target & 0x80000000) != (depth != 2): raise ValueError('Resource shape differs.')
            offset = resource_root + (target & 0x7fffffff)
            if not resource_root <= offset < resource_root + resources_size:
                raise ValueError('Resource directory outside section.')
        if offset+16 > resource_root+resources_size: raise ValueError('Truncated resource entry.')
        address, length = u32(offset), u32(offset+4)
        if address < resources_rva or address+length > resources_rva+resources_size: raise ValueError('Resource data outside section.')
        return take(rva(address, length), length)

    header = bytearray(take(0, opt+opt_size))
    for off, size in ((pe+6,2), (opt+8,4), (opt+56,4), (opt+64,4), (opt+128,8), (opt+152,4)):
        header[off:off+size] = bytes(size)
    digest = hashlib.sha256(header)
    for name, vs, va, size, raw, flags in sections:
        if name == b'.rsrc\0\0\0': continue
        digest.update(name)
        digest.update(struct.pack('<IIII',vs,0 if name == b'.reloc\0\0' else va,size,flags))
        digest.update(take(raw,size))
    digest.update(resource(24))
    return digest.hexdigest().upper(), resource


if __name__ == '__main__':
    print(inspect(pathlib.Path(sys.argv[1]).read_bytes())[0])
