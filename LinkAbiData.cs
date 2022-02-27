using System.Collections.Generic;
using EosSharp.Core.Api.v1;

namespace AnchorLinkSharp
{
    static class LinkAbiData
    {
        public static readonly Abi Types = new Abi()
        {
            version = "eosio::abi/1.1",
            types = new List<AbiType>(),
            structs = new List<AbiStruct>()
            {
                new AbiStruct()
                {
                    name = "sealed_message",
                    @base = "",
                    fields = new List<AbiField>()
                    {
                        new AbiField()
                        {
                            name = "from",
                            type = "public_key"
                        },
                        new AbiField()
                        {
                            name = "nonce",
                            type = "uint64_t"
                        },
                        new AbiField()
                        {
                            name = "ciphertext",
                            type = "bytes"
                        },
                        new AbiField()
                        {
                            name = "checksum",
                            type = "uint32"
                        }
                    }
                },
                new AbiStruct()
                {
                    name = "link_create",
                    @base = "",
                    fields = new List<AbiField>()
                    {
                        new AbiField()
                        {
                            name = "session_name",
                            type = "name"
                        },
                        new AbiField()
                        {
                            name = "request_key",
                            type = "public_key"
                        }
                    }
                }, new AbiStruct()
                {
                    name = "link_info",
                    @base = "",
                    fields = new List<AbiField>()
                    {
                        new AbiField()
                        {
                            name = "expiration",
                            type = "time_point_sec"
                        }
                    }
                }
            }
        };
    }
}
