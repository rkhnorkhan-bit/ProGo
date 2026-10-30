using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace ProGo {
    // Same canonical layout as scripts/personal_broker_image.py. This reader
    // never maps an image, executes code or loads a DLL from the inspected file.
    internal sealed class PersonalBootstrapImage {
        private readonly byte[] bytes;
        private readonly int pe,opt,optSize;
        private readonly List<Section> sections=new List<Section>();
        private readonly int resourceRoot,resourceSize;
        private sealed class Section { internal string Name; internal uint Size,Va,Raw,Flags,Virtual; }
        private byte[] Take(long offset,long length) {
            if(offset<0 || length<0 || offset>bytes.Length-length) throw new IOException("Personal native image bounds differ.");
            byte[] result=new byte[(int)length]; Buffer.BlockCopy(bytes,(int)offset,result,0,(int)length); return result;
        }
        private ushort U16(int o) { return BitConverter.ToUInt16(Take(o,2),0); }
        private uint U32(int o) { return BitConverter.ToUInt32(Take(o,4),0); }
        internal PersonalBootstrapImage(byte[] image) {
            bytes=image;
            if(System.Text.Encoding.ASCII.GetString(Take(0,2))!="MZ") throw new IOException("Personal native DOS header differs.");
            pe=checked((int)U32(60));
            if(U32(pe)!=0x00004550 || U16(pe+4)!=0x8664) throw new IOException("Personal bootstrap must be native x64.");
            int count=U16(pe+6); optSize=U16(pe+20); opt=pe+24;
            if(count<1 || count>16 || optSize!=240 || U16(opt)!=0x20b || U32(opt+108)!=16 || U32(opt+224)!=0 || U32(opt+184)!=0 || U32(opt+144)!=0)
                throw new IOException("Personal bootstrap loader configuration differs.");
            for(int i=0;i<count;i++) {
                int o=checked(opt+optSize+i*40);
                var s=new Section {Name=System.Text.Encoding.ASCII.GetString(Take(o,8)),Virtual=U32(o+8),Va=U32(o+12),Size=U32(o+16),Raw=U32(o+20),Flags=U32(o+36)};
                Take(s.Raw,s.Size);
                if(((s.Flags&0x20000000)!=0 && s.Name!=".text\0\0\0") ||
                    ((s.Name==".rsrc\0\0\0" || s.Name==".reloc\0\0") && (s.Flags&0xa0000000)!=0)) throw new IOException("Personal executable section differs.");
                foreach(var old in sections) {
                    if(old.Name==s.Name || (s.Size!=0 && old.Size!=0 && Math.Max((long)s.Raw,old.Raw)<Math.Min((long)s.Raw+s.Size,(long)old.Raw+old.Size)) ||
                        Math.Max((long)s.Va,old.Va)<Math.Min((long)s.Va+Math.Max(s.Virtual,s.Size),(long)old.Va+Math.Max(old.Virtual,old.Size))) throw new IOException("Personal image sections overlap.");
                }
                sections.Add(s);
            }
            // Normalize only placement, after binding the loader directory to
            // the unique fully hashed .reloc section. Keep its length hashed.
            Section relocation=null;
            foreach(var s in sections) if(s.Name==".reloc\0\0") relocation=s;
            uint relocRva=U32(opt+152),relocSize=U32(opt+156);
            if(relocRva!=0 || relocSize!=0 || relocation!=null) {
                if(relocation==null || relocSize==0 || relocRva!=relocation.Va ||
                    relocSize>Math.Min(relocation.Virtual,relocation.Size))
                    throw new IOException("Personal relocation directory must belong to bounded .reloc.");
                Rva(relocRva,relocSize);
            }
            uint imports=U32(opt+120);
            if(U32(opt+216)!=0) throw new IOException("Personal delay-loaded dependencies are forbidden.");
            if(imports!=0) {
                bool terminal=false;
                for(int i=0;i<64;i++) {
                    int descriptor=Rva(checked(imports+(uint)(i*20)),20); bool zero=true;
                    foreach(byte b in Take(descriptor,20)) if(b!=0) zero=false;
                    if(zero) {terminal=true;break;}
                    int name=Rva(U32(descriptor+12),1),end=name;
                    while(end<bytes.Length && end-name<128 && bytes[end]!=0) end++;
                    if(end>=bytes.Length || end-name>=128 || !String.Equals(System.Text.Encoding.ASCII.GetString(Take(name,end-name)),"kernel32.dll",StringComparison.OrdinalIgnoreCase))
                        throw new IOException("Personal bootstrap imports a non-KnownDLL dependency.");
                }
                if(!terminal) throw new IOException("Personal import inventory exceeds bounds.");
            }
            uint rva=U32(opt+128); resourceSize=checked((int)U32(opt+132)); resourceRoot=Rva(rva,(uint)resourceSize);
            int found=0; foreach(var s in sections) if(s.Name==".rsrc\0\0\0" && s.Va==rva) found++;
            if(found!=1) throw new IOException("Personal resource root differs.");
        }
        private int Rva(uint address,uint size) {
            int found=0,result=0;
            foreach(var s in sections) if(address>=s.Va && (long)address+size<=(long)s.Va+s.Size) {found++;result=checked((int)((long)s.Raw+address-s.Va));}
            if(found!=1) throw new IOException("Personal image RVA is ambiguous."); return result;
        }
        internal byte[] Resource(uint type,uint name) {
            int offset=resourceRoot;
            for(int depth=0;depth<3;depth++) {
                if((long)offset+16>(long)resourceRoot+resourceSize) throw new IOException("Personal resource directory is truncated.");
                int amount=U16(offset+12)+U16(offset+14),count=0; uint target=0;
                if(amount>128 || (long)offset+16+amount*8>(long)resourceRoot+resourceSize) throw new IOException("Personal resource exceeds bounds.");
                for(int i=0;i<amount;i++) {
                    uint id=U32(offset+16+i*8);
                    if(depth==2 || id==(depth==0?type:name)) {count++;target=U32(offset+20+i*8);}
                }
                if(count!=1 || ((target&0x80000000)!=0)!=(depth!=2)) throw new IOException("Personal resource shape differs.");
                offset=checked(resourceRoot+(int)(target&0x7fffffff));
                if(offset<resourceRoot || offset>=(long)resourceRoot+resourceSize) throw new IOException("Personal resource is outside its section.");
            }
            if((long)offset+16>(long)resourceRoot+resourceSize) throw new IOException("Personal resource entry is truncated.");
            uint address=U32(offset),size=U32(offset+4),start=U32(opt+128);
            if(address<start || (long)address+size>(long)start+resourceSize) throw new IOException("Personal resource data is outside its section.");
            return Take(Rva(address,size),size);
        }
        internal string Digest() {
            byte[] header=Take(0,opt+optSize);
            foreach(int[] field in new[] {new[]{pe+6,2},new[]{opt+8,4},new[]{opt+56,4},new[]{opt+64,4},new[]{opt+128,8},new[]{opt+152,4}})
                Array.Clear(header,field[0],field[1]);
            using(var buffer=new MemoryStream()) using(var writer=new BinaryWriter(buffer)) using(var sha=SHA256.Create()) {
                writer.Write(header);
                foreach(var s in sections) {
                    if(s.Name==".rsrc\0\0\0") continue;
                    writer.Write(System.Text.Encoding.ASCII.GetBytes(s.Name)); writer.Write(s.Virtual);
                    writer.Write(s.Name==".reloc\0\0"?0u:s.Va); writer.Write(s.Size); writer.Write(s.Flags); writer.Write(Take(s.Raw,s.Size));
                }
                writer.Write(Resource(24,1)); writer.Flush();
                return BitConverter.ToString(sha.ComputeHash(buffer.ToArray())).Replace("-","");
            }
        }
    }
}
