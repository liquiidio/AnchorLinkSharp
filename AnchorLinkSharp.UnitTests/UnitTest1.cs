using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using AnchorLinkUnityTransportSharp;
using EosioSigningRequest;
using EosSharp;
using EosSharp.Core;
using EosSharp.Core.Api.v1;
using EosSharp.Core.Helpers;
using EosSharp.Core.Interfaces;
using EosSharp.Core.Providers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Action = EosSharp.Core.Api.v1.Action;

namespace AnchorLinkSharp.UnitTests
{

    [TestClass]
    public class UnitTest1
    {
        private readonly DateTime _timestamp = new DateTime(2018, 02, 15, 00, 00, 00);

        private readonly SigningRequestEncodingOptions _options = new SigningRequestEncodingOptions()
        {
            abiSerializationProvider = new AbiSerializationProvider(new EosApi(new EosConfigurator() { HttpEndpoint = "http://eos.api.eosnation.io" }, new HttpHandler())), 
            zlib = new NetZlibProvider()
        };

        private Dictionary<string, Abi> mockAbis;

        [TestInitialize]
        public void Initialize()
        {
            mockAbis = new Dictionary<string, Abi>();
            mockAbis.Add("eosio.token", JsonConvert.DeserializeObject<Abi>("{\r\n  \"version\": \"eosio::abi/1.1\",\r\n  \"types\": [],\r\n  \"structs\": [{\r\n      \"name\": \"account\",\r\n      \"base\": \"\",\r\n      \"fields\": [{\r\n          \"name\": \"balance\",\r\n          \"type\": \"asset\"\r\n        }\r\n      ]\r\n    },{\r\n      \"name\": \"close\",\r\n      \"base\": \"\",\r\n      \"fields\": [{\r\n          \"name\": \"owner\",\r\n          \"type\": \"name\"\r\n        },{\r\n          \"name\": \"symbol\",\r\n          \"type\": \"symbol\"\r\n        }\r\n      ]\r\n    },{\r\n      \"name\": \"create\",\r\n      \"base\": \"\",\r\n      \"fields\": [{\r\n          \"name\": \"issuer\",\r\n          \"type\": \"name\"\r\n        },{\r\n          \"name\": \"maximum_supply\",\r\n          \"type\": \"asset\"\r\n        }\r\n      ]\r\n    },{\r\n      \"name\": \"currency_stats\",\r\n      \"base\": \"\",\r\n      \"fields\": [{\r\n          \"name\": \"supply\",\r\n          \"type\": \"asset\"\r\n        },{\r\n          \"name\": \"max_supply\",\r\n          \"type\": \"asset\"\r\n        },{\r\n          \"name\": \"issuer\",\r\n          \"type\": \"name\"\r\n        }\r\n      ]\r\n    },{\r\n      \"name\": \"issue\",\r\n      \"base\": \"\",\r\n      \"fields\": [{\r\n          \"name\": \"to\",\r\n          \"type\": \"name\"\r\n        },{\r\n          \"name\": \"quantity\",\r\n          \"type\": \"asset\"\r\n        },{\r\n          \"name\": \"memo\",\r\n          \"type\": \"string\"\r\n        }\r\n      ]\r\n    },{\r\n      \"name\": \"open\",\r\n      \"base\": \"\",\r\n      \"fields\": [{\r\n          \"name\": \"owner\",\r\n          \"type\": \"name\"\r\n        },{\r\n          \"name\": \"symbol\",\r\n          \"type\": \"symbol\"\r\n        },{\r\n          \"name\": \"ram_payer\",\r\n          \"type\": \"name\"\r\n        }\r\n      ]\r\n    },{\r\n      \"name\": \"retire\",\r\n      \"base\": \"\",\r\n      \"fields\": [{\r\n          \"name\": \"quantity\",\r\n          \"type\": \"asset\"\r\n        },{\r\n          \"name\": \"memo\",\r\n          \"type\": \"string\"\r\n        }\r\n      ]\r\n    },{\r\n      \"name\": \"transfer\",\r\n      \"base\": \"\",\r\n      \"fields\": [{\r\n          \"name\": \"from\",\r\n          \"type\": \"name\"\r\n        },{\r\n          \"name\": \"to\",\r\n          \"type\": \"name\"\r\n        },{\r\n          \"name\": \"quantity\",\r\n          \"type\": \"asset\"\r\n        },{\r\n          \"name\": \"memo\",\r\n          \"type\": \"string\"\r\n        }\r\n      ]\r\n    }\r\n  ],\r\n  \"actions\": [{\r\n      \"name\": \"close\",\r\n      \"type\": \"close\",\r\n      \"ricardian_contract\": \"\"\r\n    },{\r\n      \"name\": \"create\",\r\n      \"type\": \"create\",\r\n      \"ricardian_contract\": \"\"\r\n    },{\r\n      \"name\": \"issue\",\r\n      \"type\": \"issue\",\r\n      \"ricardian_contract\": \"\"\r\n    },{\r\n      \"name\": \"open\",\r\n      \"type\": \"open\",\r\n      \"ricardian_contract\": \"\"\r\n    },{\r\n      \"name\": \"retire\",\r\n      \"type\": \"retire\",\r\n      \"ricardian_contract\": \"\"\r\n    },{\r\n      \"name\": \"transfer\",\r\n      \"type\": \"transfer\",\r\n      \"ricardian_contract\": \"## Transfer Terms & Conditions\\n\\nI, {{from}}, certify the following to be true to the best of my knowledge:\\n\\n1. I certify that {{quantity}} is not the proceeds of fraudulent or violent activities.\\n2. I certify that, to the best of my knowledge, {{to}} is not supporting initiation of violence against others.\\n3. I have disclosed any contractual terms & conditions with respect to {{quantity}} to {{to}}.\\n\\nI understand that funds transfers are not reversible after the {{transaction.delay}} seconds or other delay as configured by {{from}}'s permissions.\\n\\nIf this action fails to be irreversibly confirmed after receiving goods or services from '{{to}}', I agree to either return the goods or services or resend {{quantity}} in a timely manner.\\n\"\r\n    }\r\n  ],\r\n  \"tables\": [{\r\n      \"name\": \"accounts\",\r\n      \"index_type\": \"i64\",\r\n      \"key_names\": [],\r\n      \"key_types\": [],\r\n      \"type\": \"account\"\r\n    },{\r\n      \"name\": \"stat\",\r\n      \"index_type\": \"i64\",\r\n      \"key_names\": [],\r\n      \"key_types\": [],\r\n      \"type\": \"currency_stats\"\r\n    }\r\n  ],\r\n  \"ricardian_clauses\": [],\r\n  \"error_messages\": [],\r\n  \"abi_extensions\": [],\r\n  \"variants\": []\r\n}"));
        }

        [TestMethod]
        public void ShouldCreateFromAction()
        {
            var requestAData = (SigningRequest.create(new SigningRequestCreateArguments()
            {
                action = new Action()
                {
                    account = "eosio.token",
                    name = "transfer",
                    authorization = new List<PermissionLevel>() { new() { actor = "foo", permission = "active" } },
                    data = new { from = "foo", to = "bar", quantity = "1.000 EOS", memo = "hello there" },
                },
            },
                _options
            ).Result).data;

            var requestBData = new SigningRequestData()
            {
                chain_id = new KeyValuePair<string, object>("chain_alias", 1),
                req = new KeyValuePair<string, object>("action", new
                {
                    account = "eosio.token",
                    name = "transfer",
                    authorization = new List<PermissionLevel>() { new() { actor = "foo", permission = "active" } },
                    data = new { from = "foo", to = "bar", quantity = "1.000 EOS", memo = "hello there" },
                    hex_data = "000000000000285d000000000000ae39e80300000000000003454f53000000000b68656c6c6f207468657265",
                }),
                callback = "",
                flags = 1,
                info = new List<object>(),
            };

            Console.WriteLine(JsonConvert.SerializeObject(requestAData));
            Console.WriteLine(JsonConvert.SerializeObject(requestBData));

            // Comparing Json-Strings because of anonymous objects comparison will always return false
            Assert.IsTrue(JToken.DeepEquals(JToken.FromObject(requestAData), JToken.FromObject(requestBData)));
        }

        [TestMethod]
        public void ShouldCreateFromActions()
        {
            var requestAData = (SigningRequest.create(new SigningRequestCreateArguments()
                {
                    callback = new KeyValuePair<string,bool>("https=//example.com/?tx={{tx}}", true),
                    actions = new Action[]
                        {
                        new Action()
                        {
                            account = "eosio.token",
                            name = "transfer",
                            authorization = new List<PermissionLevel>() {new() {actor = "foo", permission = "active"}},
                            data = new {from = "foo", to = "bar", quantity = "1.000 EOS", memo = "hello there"},
                        },
                        new Action()
                        {
                            account = "eosio.token",
                            name = "transfer",
                            authorization = new List<PermissionLevel>() {new() {actor = "baz", permission = "active"}},
                            data = new {from = "baz", to = "bar", quantity = "1.000 EOS", memo = "hello there"},
                        },
                        }
                }, 
                _options
            ).Result).data;

            var requestBData = new SigningRequestData()
            {
                chain_id = new KeyValuePair<string, object>("chain_alias", 1),
                req = new KeyValuePair<string, object>("action[]", new[]
                    {
                    new
                    {
                        account = "eosio.token",
                        name = "transfer",
                        authorization = new List<PermissionLevel>() { new() { actor = "foo", permission = "active" } },
                        data = new {from = "foo", to = "bar", quantity = "1.000 EOS", memo = "hello there"},
                        hex_data = "000000000000285d000000000000ae39e80300000000000003454f53000000000b68656c6c6f207468657265",
                    },
                    new
                    {
                        account = "eosio.token",
                        name = "transfer",
                        authorization = new List<PermissionLevel>() { new() { actor = "baz", permission = "active" } },
                        data = new {from = "baz", to = "bar", quantity = "1.000 EOS", memo = "hello there"},
                        hex_data = "000000000000be39000000000000ae39e80300000000000003454f53000000000b68656c6c6f207468657265",
                    }
                }
                ),
                callback = "https=//example.com/?tx={{tx}}",
                flags = 3,
                info = new List<object>(),
            };

            Console.WriteLine(JsonConvert.SerializeObject(requestAData));
            Console.WriteLine(JsonConvert.SerializeObject(requestBData));

            Assert.IsTrue(JToken.DeepEquals(JToken.FromObject(requestAData), JToken.FromObject(requestBData)));
        }

        [TestMethod]
        public void ShouldCreateFromTransaction() {


            var requestAData = (SigningRequest.create(new SigningRequestCreateArguments()
                {
                    callback = "https=//example.com/?tx={{tx}}",
                    transaction = new Transaction()
                    {
                        delay_sec = 123,
                        expiration = _timestamp,
                        max_cpu_usage_ms = 99,
                        actions = new List<Action>()
                        {
                            new Action()
                            {
                                account = "eosio.token",
                                name = "transfer",
                                authorization = new List<PermissionLevel>(){ new PermissionLevel(){ actor = "foo", permission = "active"}},
                                hex_data = "000000000000285D000000000000AE39E80300000000000003454F53000000000B68656C6C6F207468657265",
                                data = new {}
                            }
                        }
                    }, 
                    broadcast = false
                },
                _options
            ).Result).data;


            var requestBData = new SigningRequestData()
            {
                chain_id = new KeyValuePair<string, object>("chain_alias", 1),
                req = new KeyValuePair<string, object>("transaction", new 
                    {
                        actions = new[]
                        {
                            new {
                                account = "eosio.token",
                                name = "transfer",
                                authorization = new List<PermissionLevel>(){ new PermissionLevel(){ actor = "foo", permission = "active"}},
                                hex_data = "000000000000285D000000000000AE39E80300000000000003454F53000000000B68656C6C6F207468657265",
                                data = new {}
                            }
                        },
                        context_free_actions = new List<object>(),
                        delay_sec = 123,
                        expiration = _timestamp,
                        max_cpu_usage_ms = 99,
                        max_net_usage_words = 0,
                        ref_block_num = 0,
                        ref_block_prefix = 0,
                        transaction_extensions = new List<object>(),
                    }
                ),
                callback = "https=//example.com/?tx={{tx}}",
                flags = 0,
                info = new List<object>(),
            };

            Console.WriteLine(JsonConvert.SerializeObject(requestAData));
            Console.WriteLine(JsonConvert.SerializeObject(requestBData));

            Assert.IsTrue(JToken.DeepEquals(JToken.FromObject(requestAData), JToken.FromObject(requestBData)));
        }

        [TestMethod]
        public void ShouldCreateFromUri()
        {
            var requestAData = SigningRequest.from(
                "esr://gmNgZGBY1mTC_MoglIGBIVzX5uxZRqAQGMBoExgDAjRi4fwAVz93ICUckpGYl12skJZfpFCSkaqQllmcwczAAAA",
                _options
            ).data;

            var requestBData = new {
                chain_id = new KeyValuePair<string, object>("chain_alias", 1),
                req = new KeyValuePair<string, object>("action", new
                {
                    account = "eosio.token",
                    name = "transfer",
                    authorization = new List<PermissionLevel>() { new PermissionLevel() { actor = "............1", permission= "............1"}},
                    data = "AQAAAAAAAAAAAAAAAAAoXQEAAAAAAAAAAFBFTkcAAAATVGhhbmtzIGZvciB0aGUgZmlzaA==",
                    hex_data = "0100000000000000000000000000285d01000000000000000050454e47000000135468616e6b7320666f72207468652066697368",
                }),
                flags = 3,
                callback = "",
                info = new List<InfoPair>(),
            };

            Console.WriteLine(JsonConvert.SerializeObject(requestAData));
            Console.WriteLine(JsonConvert.SerializeObject(requestBData));

            Assert.AreEqual(JsonConvert.SerializeObject(requestAData), JsonConvert.SerializeObject(requestBData));
        }

        [TestMethod]
        public void ShouldResolveToTransaction()
        {
            var requestAData = SigningRequest.create(new SigningRequestCreateArguments()
                {
                    action = new Action()
                    {
                        account = "eosio.token",
                        name = "transfer",
                        authorization = new List<PermissionLevel>()
                            { new PermissionLevel() { actor = "foo", permission = "active" } },
                        data = new { from = "foo", to = "bar", quantity = "1.000 EOS", memo = "hello there" },
                    },
                },
                _options
            ).Result;

            var abis = requestAData.fetchAbis(_options.abiSerializationProvider).Result;
            var tx = requestAData.resolveTransaction(
                abis, new PermissionLevel() { actor = "foo", permission = "bar" }, new TransactionContext()
                {
                    timestamp = _timestamp,
                    block_num = 1234,
                    expire_seconds = 0,
                    ref_block_prefix = 56789,
                }
            );

            var requestBData = new Transaction()
            {
                actions = new List<Action>()
                {
                    new Action()
                    {
                        account = "eosio.token",
                        name = "transfer",
                        authorization = new List<PermissionLevel>()
                            { new PermissionLevel() { actor = "foo", permission = "active" } },
                        data = new { from = "foo", to = "bar", quantity = "1.000 EOS", memo = "hello there" },
                    }
                },
                context_free_actions = new List<Action>(),
                transaction_extensions = new List<Extension>(),
                expiration = _timestamp,
                ref_block_num = 1234,
                ref_block_prefix = 56789,
                max_cpu_usage_ms = 0,
                max_net_usage_words = 0,
                delay_sec = 0,
            };

            Console.WriteLine(JsonConvert.SerializeObject(tx));
            Console.WriteLine(JsonConvert.SerializeObject(requestBData));

            Assert.IsTrue(JToken.DeepEquals(JToken.FromObject(tx), JToken.FromObject(requestBData)));
        }

        [TestMethod]
        public void ShouldResolveWithPlaceholderName() {

            /*
             * Automatic Placeholder Replacement only works if a Dictionary<string, object> is used for action.data
             */

            var requestAData = SigningRequest.create(new SigningRequestCreateArguments()
                {
                    action = new Action()
                    {
                        account = "eosio.token",
                        name = "transfer",
                        authorization = new List<PermissionLevel>() { Constants.PlaceholderAuth },
                        data = new Dictionary<string, object>()
                        {
                            { "from", "............1" },
                            { "to", "............2" },
                            { "quantity", "1.000 EOS" },
                            { "memo", "hello there" },
                        },
                    },
                },
                _options
            ).Result;

            var abis = requestAData.fetchAbis(_options.abiSerializationProvider).Result;
            var tx = requestAData.resolveTransaction(abis, new PermissionLevel() { actor = "foo", permission = "mractive" },
                new TransactionContext()
                {
                    timestamp = _timestamp,
                    block_num = 1234,
                    expire_seconds = 0,
                    ref_block_prefix = 56789,
                }
            );

            var requestBData = new Transaction()
            {
                actions = new List<Action>
                {
                    new Action()
                    {
                        account = "eosio.token",
                        name = "transfer",
                        authorization = new List<PermissionLevel>()
                            { new PermissionLevel() { actor = "foo", permission = "mractive" } },
                        data = new { from = "foo", to = "mractive", quantity = "1.000 EOS", memo = "hello there" },
                    }
                },
                context_free_actions = new List<Action>(),
                transaction_extensions = new List<Extension>(),
                expiration = _timestamp,
                ref_block_num = 1234,
                ref_block_prefix = 56789,
                max_cpu_usage_ms = 0,
                max_net_usage_words = 0,
                delay_sec = 0,
            };

            Console.WriteLine(JsonConvert.SerializeObject(tx));
            Console.WriteLine(JsonConvert.SerializeObject(requestBData));

            Assert.IsTrue(JToken.DeepEquals(JToken.FromObject(tx), JToken.FromObject(requestBData)));
        }

        [TestMethod]
        public void ShouldEncodeAndDecodeRequests()
        {
            var req1 = SigningRequest.create(new SigningRequestCreateArguments()
                {
                    callback = new KeyValuePair<string, bool>("", true),
                    action = new Action()
                    {
                        account = "eosio.token",
                        name = "transfer",
                        authorization = new List<PermissionLevel>()
                        {
                            new PermissionLevel()
                                { actor = Constants.PlaceholderName, permission = Constants.PlaceholderName }
                        },
                        data = new Dictionary<string, object>()
                        {
                            { "from", Constants.PlaceholderName },
                            { "to", "foo" },
                            { "quantity", "1. PENG" },
                            { "memo", "Thanks for the fish" },
                        },
                    },
                },
                _options
            ).Result;

            var encoded = req1.encode();

            Console.WriteLine(encoded);
            Console.WriteLine("esr://gmNgZGBY1mTC_MoglIGBIVzX5uxZRqAQGMBoExgDAjRi4fwAVz93ICUckpGYl12skJZfpFCSkaqQllmcwczAAAA");
            Console.WriteLine(Environment.NewLine);

            Assert.AreEqual(encoded, "esr://gmNgZGBY1mTC_MoglIGBIVzX5uxZRqAQGMBoExgDAjRi4fwAVz93ICUckpGYl12skJZfpFCSkaqQllmcwczAAAA");

            var req2 = SigningRequest.from(encoded, _options);

            Console.WriteLine(JsonConvert.SerializeObject(req1.data));
            Console.WriteLine(JsonConvert.SerializeObject(req2.data));
            Console.WriteLine(Environment.NewLine);

            Assert.AreEqual(JsonConvert.SerializeObject(req1.data), JsonConvert.SerializeObject(req2.data));
        }


        [TestMethod]
        public void ShouldCreateIdentityTx()
        {
            var req = SigningRequest.identity(new SigningRequestCreateIdentityArguments()
                {
                    callback = new KeyValuePair<string, bool>("https=//example.com", true)
                },
                _options
            );

            var resolvedTrx = req.resolveTransaction(mockAbis, new PermissionLevel()
            {
                actor = "foo",
                permission = "bar",
            });

            var trx = new Transaction()
            {
                actions = new List<Action>()
                {
                    new Action()
                    {
                        account = "",
                        name = "identity",
                        authorization = new List<PermissionLevel>() { new PermissionLevel() { actor = "foo", permission = "bar" } },
                        data = new Dictionary<string, object>()
                        {
                            { "permission", new Dictionary<string, object>()
                                {
                                    { "actor", "foo" },
                                    { "permission", "bar" }
                                }
                            }
                        }
                    },
                },
                context_free_actions = new List<Action>(),
                transaction_extensions = new List<Extension>(),
                expiration = new DateTime(1970, 1, 1),
                ref_block_num = 0,
                ref_block_prefix = 0,
                max_cpu_usage_ms = 0,
                max_net_usage_words = 0,
                delay_sec = 0,
            };

            Console.WriteLine(JsonConvert.SerializeObject(resolvedTrx));
            Console.WriteLine(JsonConvert.SerializeObject(trx));
            Console.WriteLine(Environment.NewLine);

            Assert.AreEqual(JsonConvert.SerializeObject(resolvedTrx), JsonConvert.SerializeObject(trx));

            var resolvedTrx2 = req.resolveTransaction(mockAbis, new PermissionLevel()
            {
                actor = "other",
                permission = "active",
            });

            Console.WriteLine(JsonConvert.SerializeObject(resolvedTrx.actions[0].data));
            Console.WriteLine(JsonConvert.SerializeObject(resolvedTrx2.actions[0].data));
            Console.WriteLine(Environment.NewLine);

            Assert.AreNotEqual(JsonConvert.SerializeObject(resolvedTrx.actions[0].data), JsonConvert.SerializeObject(resolvedTrx2.actions[0].data));

            //assert.notStrictEqual(recode(tx2.actions[0].data), recode(tx.actions[0].data))
        }

        class MockSignProvider : ISignProvider
        {
            private string _signer;
            private string _signature;
            public MockSignProvider(string signer, string signature)
            {
                this._signer = signer;
                this._signature = signature;
            }

            public Task<IEnumerable<string>> GetAvailableKeys()
            {
                throw new NotImplementedException();
            }

            public Task<IEnumerable<string>> Sign(string chainId, IEnumerable<string> requiredKeys, byte[] signBytes, IEnumerable<string> abiNames = null)
            {
                throw new NotImplementedException();
            }

            public string Sign(string chainId, byte[] signBytes)
            {
                return _signature;
            }

            public Dictionary<string, string> Sign()
            {
                return new Dictionary<string, string>()
                {
                    { "signature", _signature },
                    { "signer", _signer }
                };
            }
        }

        [TestMethod]
        public void ShouldEncodeAndDecodeSignedRequests()
        {
            var mockSignature = new MockSignProvider("foo",
                "SIG_K1_K8Wm5AXSQdKYVyYFPCYbMZurcJQXZaSgXoqXAKE6uxR6Jot7otVzS55JGRhixCwNGxaGezrVckDgh88xTsiu4wzzZuP9JE");

            var options = new SigningRequestEncodingOptions()
            {
                abiSerializationProvider = new AbiSerializationProvider(new EosApi(
                    new EosConfigurator() { HttpEndpoint = "http://eos.api.eosnation.io" }, new HttpHandler())),
                zlib = new NetZlibProvider(),
                signatureProvider = mockSignature
            };

            var req1 = SigningRequest.create(new SigningRequestCreateArguments()
                {
                    action = new Action()
                    {
                        account = "eosio.token",
                        name = "transfer",
                        authorization = new List<PermissionLevel>()
                            { new PermissionLevel() { actor = "foo", permission = "active" } },
                        data = new { from = "foo", to = "bar", quantity = "1.000 EOS", memo = "hello there" },
                    },
                },
                options //, signatureProvider}
            ).Result;

            // assert.deepStrictEqual(recode(req1.signature), mockSig)
            var encoded = req1.encode();

            Console.WriteLine(encoded);
            Console.WriteLine(
                "esr://gmNgZGBY1mTC_MoglIGBIVzX5uxZoAgIaMSCyBVvjYx0kAUYGNZZvmCGsJhd_YNBNHdGak5OvkJJRmpRKlQ3WLl8anjWFNWd23XWfvzTcy_qmtRx5mtMXlkSC23ZXle6K_NJFJ4SVTb4O026Wb1G5Wx0u1A3-_G4rAPsBp78z9lN7nddAQA");

            Assert.AreEqual(encoded,
                "esr://gmNgZGBY1mTC_MoglIGBIVzX5uxZoAgIaMSCyBVvjYx0kAUYGNZZvmCGsJhd_YNBNHdGak5OvkJJRmpRKlQ3WLl8anjWFNWd23XWfvzTcy_qmtRx5mtMXlkSC23ZXle6K_NJFJ4SVTb4O026Wb1G5Wx0u1A3-_G4rAPsBp78z9lN7nddAQA");

            var req2 = SigningRequest.from(encoded, _options);
            Assert.AreEqual(JsonConvert.SerializeObject(req2.data), JsonConvert.SerializeObject(req1.data));

//            Assert.AreEqual(JsonConvert.SerializeObject(req2.signature), JsonConvert.SerializeObject(mockSignature.Sign()));}
        }

        [TestMethod]
        public void ShouldEncodeAndDecodeTestRequests()
        {
            var req1uri =
                "esr://gmNgZGBY1mTC_MoglIGBIVzX5uxZRqAQGMBoExgDAjRi4fwAVz93ICUckpGYl12skJZfpFCSkaqQllmcwczAAAA";
            var req2uri =
                "esr://gmNgZGBY1mTC_MoglIGBIVzX5uxZRqAQGMBoExgDAjRi4fwAVz93ICUckpGYl12skJZfpFCSkaqQllmcwQxREVOsEcsgX-9-jqsy1EhNQM_GM_FkQMIziUU1VU4PsmOn_3r5hUMumeN3PXvdSuWMm1o9u6-FmCwtPvR0haqt12fNKtlWzTuiNwA";
            
            var req1 = SigningRequest.from(req1uri, _options);
            var req2 = SigningRequest.from(req2uri, _options);

            Console.WriteLine(JsonConvert.SerializeObject(req1));
            Console.WriteLine(JsonConvert.SerializeObject(req2));
            Console.Write(Environment.NewLine);

            var req1resolved = req1.resolveActions(mockAbis, null);
            var req2resolved = req2.resolveActions(mockAbis, null);
            Assert.AreEqual(JsonConvert.SerializeObject(req1resolved), JsonConvert.SerializeObject(req2resolved));

            var req1Sig = req1.signature;
            Assert.IsNull(req1Sig);

            var req2Sig = req2.signature;
            var req3Sig = new RequestSignature()
            {
                signer = "foobar",
                signature =
                    "SIG_K1_KBub1qmdiPpWA2XKKEZEG3EfKJBf38GETHzbd4t3CBdWLgdvFRLCqbcUsBbbYga6jmxfdSFfodMdhMYraKLhEzjSCsiuMs"
            };
            Console.WriteLine(JsonConvert.SerializeObject(req2Sig));
            Console.WriteLine(JsonConvert.SerializeObject(req3Sig));
            Console.Write(Environment.NewLine);

            Assert.AreEqual(JsonConvert.SerializeObject(req2Sig), JsonConvert.SerializeObject(req3Sig));

            Assert.AreEqual(req1.encode(), req1uri);
            Assert.AreEqual(req2.encode(), req2uri);
        }

        [TestMethod]
        public void ShouldGenerateCorrectIdentityRequests()
        {
            var reqUri = "esr://AgABAwACJWh0dHBzOi8vY2guYW5jaG9yLmxpbmsvMTIzNC00NTY3LTg5MDAA";
            var req = SigningRequest.from(reqUri, _options);
            Assert.AreEqual(req.isIdentity(), true);
            Assert.AreEqual(req.getIdentity(), null);
            Assert.AreEqual(req.getIdentityPermission(), null);
            Assert.AreEqual(req.encode(), reqUri);

            var resolved = req.resolve(new Dictionary<string, Abi>(),
                new PermissionLevel() { actor = "foo", permission = "bar" }, null);

            var resolvedTrx = resolved.transaction;

            var trx2 = new Transaction()
            {
                actions = new List<Action>
                {
                    new Action()
                    {
                        account = "",
                        name = "identity",
                        authorization = new List<PermissionLevel>()
                        {
                            new PermissionLevel()
                            {
                                actor = "foo",
                                permission = "bar",
                            }
                        },
                        data = new Dictionary<string, object>
                        {
                            {
                                "permission", new Dictionary<string, object>()
                                {
                                    { "actor", "foo" },
                                    { "permission", "bar" }
                                }
                            }
                        }
                    }
                },
                context_free_actions = new List<Action>(),
                delay_sec = 0,
                expiration = new DateTime(1970, 1, 1),
                max_cpu_usage_ms = 0,
                max_net_usage_words = 0,
                ref_block_num = 0,
                ref_block_prefix = 0,
                transaction_extensions = new List<Extension>()
            };

            Assert.AreEqual(JsonConvert.SerializeObject(resolvedTrx), JsonConvert.SerializeObject(trx2));
        }

        [TestMethod]
        public void ShouldEncodeAndDecodeWithMetadata()
        {
            var abiSerializationProvider = new AbiSerializationProvider();
            var data = abiSerializationProvider.Serialize("hello", "string");
            var req = SigningRequest.identity(new SigningRequestCreateIdentityArguments()
                {
                    callback = "https=//example.com",
                    info = new List<InfoPair>()
                    {
                        new InfoPair()
                        {
                            key = "foo",
                            value = "bar"
                        },
                        new InfoPair()
                        {
                            key = "baz",
                            value = data,
                        }

                    },
                    
                },
                _options
            );
            // TODO
            req.setInfoKey(
                "extra_sig",
                "SIG_K1_K4nkCupUx3hDXSHq4rhGPpDMPPPjJyvmF3M6j7ppYUzkR3L93endwnxf3YhJSG4SSvxxU1ytD8hj39kukTeYxjwy5H3XNJ",
                "signature"
            );
            var encoded = req.encode();
            var decoded = SigningRequest.from(encoded, _options);
            // TODO
            Assert.AreEqual(SerializationHelper.ByteArrayToHexString(decoded.getRawInfo()["foo"]), SerializationHelper.ByteArrayToHexString(req.getRawInfo()["foo"]));
            Assert.AreEqual(decoded.getInfos()["foo"], "bar");
            Assert.AreEqual(decoded.getInfos()["baz"], "hello");
            Assert.AreEqual(decoded.getInfo("extra_sig","signature"),
                "SIG_K1_K4nkCupUx3hDXSHq4rhGPpDMPPPjJyvmF3M6j7ppYUzkR3L93endwnxf3YhJSG4SSvxxU1ytD8hj39kukTeYxjwy5H3XNJ");
        }

        [TestMethod]
        public void ShouldTemplateCallbackUrl()
        {
            var mockSig =
                "SIG_K1_K8Wm5AXSQdKYVyYFPCYbMZurcJQXZaSgXoqXAKE6uxR6Jot7otVzS55JGRhixCwNGxaGezrVckDgh88xTsiu4wzzZuP9JE";
            var mockTx = "308d206c51c5dd6c02e0417e44560cdc2e76db7765cea19dfa8f9f94922f928a";
            var request = SigningRequest.create(new SigningRequestCreateArguments()
                {
                    action = new Action()
                    {
                        account = "eosio.token",
                        name = "transfer",
                        authorization = new List<PermissionLevel>()
                            { new PermissionLevel() { actor = "foo", permission = "active" } },
                        data = new Dictionary<string, object>()
                        {
                            { "from", "foo" },
                            { "to", "bar" },
                            { "quantity", "1.000 EOS" },
                            { "memo", "hello there" }
                        },
                    },
                    callback = "https://example.com/?sig={{sig}}&tx={{tx}}",
                },
                _options
            ).Result;

            var abis = request.fetchAbis(_options.abiSerializationProvider).Result;

            var resolved = request.resolve(
                abis, new PermissionLevel() { actor = "foo", permission = "bar" },
                new TransactionContext()
                {
                    timestamp = _timestamp,
                    block_num = 1234,
                    expire_seconds = 0,
                    ref_block_prefix = 56789,
                }
            );

            var callback = resolved.getCallback(new[] { mockSig }, null);
            var expected = $"https://example.com/?sig={mockSig}&tx={mockTx}";

            Console.WriteLine(callback.url);
            Console.WriteLine(expected);
            Assert.AreEqual(callback.url, expected);
        }

        [TestMethod]
        public void ShouldDeepClone()
        {
            var request = SigningRequest.create(new SigningRequestCreateArguments()
                {
                    action = new Action()
                    {
                        account = "eosio.token",
                        name = "transfer",
                        authorization = new List<PermissionLevel>()
                            { new PermissionLevel() { actor = "foo", permission = "active" } },
                        data = new Dictionary<string, object>()
                        {
                            { "from", "foo" },
                            { "to", "bar" },
                            { "quantity", "1.000 EOS" },
                            { "memo", "" }
                        },
                    },
                },
                _options
            ).Result;
            var copy = request.clone();

            Assert.AreEqual(JsonConvert.SerializeObject(request.data), JsonConvert.SerializeObject(copy.data));

            Assert.AreEqual(request.encode(), copy.encode());

            copy.setInfoKey("foo", true);
            Assert.AreNotEqual(request.data, copy.data);
            Assert.AreEqual(request.encode(), copy.encode());
        }

        [TestMethod]
        public void ShouldResolveTemplatedCallbackUrls()
        {
            var req1uri =
                "esr://gmNgZGBY1mTC_MoglIGBIVzX5uxZRqAQGDBBaUWYAARoxMIkGAJDIyAM9YySkoJiK3391IrE3IKcVL3k_Fz7kgrb6uqSitpataQ8ICspr7aWAQA";
            var req1 = SigningRequest.from(req1uri, _options);
            var abis = req1.fetchAbis(_options.abiSerializationProvider).Result;
            var resolved = req1.resolve(
                abis, new PermissionLevel() { actor = "foo", permission = "bar" }, new TransactionContext()
                {
                    timestamp = _timestamp,
                    block_num = 1234,
                    expire_seconds = 0,
                    ref_block_prefix = 56789
                }
            );
            
            var callback = resolved.getCallback(new[]
            {
                "SIG_K1_KBub1qmdiPpWA2XKKEZEG3EfKJBf38GETHzbd4t3CBdWLgdvFRLCqbcUsBbbYga6jmxfdSFfodMdhMYraKLhEzjSCsiuMs",
            }, 1234);

            var expected =
                "https://example.com?tx=6aff5c203810ff6b40469fe20318856354889ff037f4cf5b89a157514a43e825&bn=1234";
            Assert.AreEqual(expected, callback!.url);

        }

        [TestMethod]
        [Ignore]
        public void ShouldHandleScopedIdRequests()
        {

            var scope = SerializationHelper.ConvertULongToName(18446744073709551615)!;
            var req = SigningRequest.create(new SigningRequestCreateArguments()
                {
                    Identity = new IdentityV3() { scope = scope },
                    callback = new KeyValuePair<string, bool>("https://example.com", true),
                },
                _options
            ).Result;
            Assert.AreEqual("esr://g2NgZP4PBQxMwhklJQXFVvr6qRWJuQU5qXrJ-bkMAA", req.encode());

            //Expected: <esr://g2NgZP4PBQxMwhklJQXFVvr6qRWJuQU5qXrJ-bkMAA>.
            //Actual:   <esr://g2NgZGb4DwVMwhklJQXFVvr6qRWJuQU5qXrJ-bkMAA>. 


            //Expected: <esr://g2NgZP4PBQxMwhklJQXFVvr6qRWJuQU5qXrJ-bkMAA>.
            //Actual:   <esr://g2NgZGb4DwVMwhklJQXFVvr6qRWJuQU5qXrJ-bkMAA>. 


            //Expected: <esr://g2NgZP4PBQxMwhklJQXFVvr6qRWJuQU5qXrJ-bkMAA>.
            //Actual:   <esr://g2NgZGb4DwVMwhklJQXFVvr6qRWJuQU5qXrJ-bkMAA>. 

            //Expected: <esr://g2NgZP4PBQxMwhklJQXFVvr6qRWJuQU5qXrJ-bkMAA>.
            //Actual:   <esr://g2NgZGb49x8CmIQzSkoKiq309VMrEnMLclL1kvNzGQA>. 

            //Expected: <esr://g2NgZP4PBQxMwhklJQXFVvr6qRWJuQU5qXrJ-bkMAA>.
            //Actual:   <esr://g2NgZGb4DwVMwhklJQXFVvr6qRWJuQU5qXrJ-bkMAA>. 

            //Expected: <esr://g2NgZP4PBQxMwhklJQXFVvr6qRWJuQU5qXrJ-bkMAA>.
            //Actual:   <esr://g2NgZGb48B8CmIQzSkoKiq309VMrEnMLclL1kvNzGQA>. 

            //Expected: <esr://g2NgZP4PBQxMwhklJQXFVvr6qRWJuQU5qXrJ-bkMAA>.
            //Actual:   <esr://g2NgZGb4DwVMwhklJQXFVvr6qRWJuQU5qXrJ-bkMAA>. 

            //Expected: <esr://g2NgZP4PBQxMwhklJQXFVvr6qRWJuQU5qXrJ-bkMAA>.
            //Actual:   <esr://g2NgZGb48B8CmIQzSkoKiq309VMrEnMLclL1kvNzGQA>. 


            var decoded = SigningRequest.from(
                "esr://g2NgZP4PBQxMwhklJQXFVvr6qRWJuQU5qXrJ-bkMAA",
                _options
            );
            Assert.AreEqual(decoded.data.Equals(req.data), true);

            Assert.AreEqual(decoded.getIdentityScope(), scope);

            var resolved = req.resolve(
                new Dictionary<string, Abi>(), new PermissionLevel() { actor = "foo", permission = "active" },
                new TransactionContext()
                {
                    expiration = new DateTime(2020, 07, 10, 08, 40, 20)
                }
            );
            Assert.AreEqual(resolved.transaction.expiration, new DateTime(2020, 07, 10, 08, 40, 20));
            Assert.AreEqual(
                resolved.transaction.actions[0].hex_data,
                "ffffffffffffffff01000000000000285d00000000a8ed3232"
            );

            Assert.Equals(SerializationHelper.ByteArrayToHexString(req.getSignatureDigest()),
                "70d1fd5bda1998135ed44cbf26bd1cc2ed976219b2b6913ac13f41d4dd013307"
            );
        }

        [TestMethod]
        public void ShouldHandleMultiChainIdRequests()
        {
            //var req = SigningRequest.identity(new SigningRequestCreateIdentityArguments()
            //{
            //    chainId = null,
            //    //chainIds =  [ChainName.EOS, ChainName.WAX],
            //    //scope = "foo",
            //    callback = new KeyValuePair<string, bool>("myapp=//login={{cid}}", false)
            //});

            //assert.equal(req.isMultiChain(), true)
            //assert.deepEqual(req.getChainIds()!.map(String), [
            //    "aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906",
            //    "1064487b3cd1a897ce03ae5b6a865651747e2e152090f99c1d19d44e01aea5a4",
            //])

            //var resolved = req.resolve(
            //    new Dictionary<string, Abi>(), new PermissionLevel() { actor = "foo", permission = "active" },
            //    new TransactionContext()
            //    {
            //        expiration = new DateTime(2020, 07, 10, 08, 40, 20),
            //        chainId = "1064487b3cd1a897ce03ae5b6a865651747e2e152090f99c1d19d44e01aea5a4",
            //    }
            //);

            //var key = PrivateKey.from("PVT_K1_2wFL8Ne8JoGrxz6GdnfB7d4yhUYpqNgubHeKUC64qT3XE6Ro84");
            //var sig = key.signDigest(resolved.signingDigest);
            //var callback = resolved.getCallback(new[] { sig });
            ////assert.deepEqual(callback, 
            //var callback2 = new ResolvedCallback()
            //{
            //    background = false,
            //    payload = new CallbackPayload()
            //    {
            //        sig =
            //            "SIG_K1_K4nkCupUx3hDXSHq4rhGPpDMPPPjJyvmF3M6j7ppYUzkR3L93endwnxf3YhJSG4SSvxxU1ytD8hj39kukTeYxjwy5H3XNJ",
            //        tx = "b8e921a7b68d7309847e633d74963f25eb5a7d0b15b1aceb143723c234686a8d",
            //        rbn = "0",
            //        rid = "0",
            //        ex = "2020-07-10T08=40=20",
            //        req = "esr=//AwAAAwAAAAAAAChdAAAVbXlhcHA6Ly9sb2dpbj17e2NpZH19AQljaGFpbl9pZHMFAgABAAo",
            //        sa = "foo",
            //        sp = "active",
            //        cid = "1064487b3cd1a897ce03ae5b6a865651747e2e152090f99c1d19d44e01aea5a4",
            //    },
            //    url = "myapp=//login=1064487b3cd1a897ce03ae5b6a865651747e2e152090f99c1d19d44e01aea5a4",
            //};
            //var recreated = ResolvedSigningRequest.fromPayload(callback.payload,_options, _options.abiProvider).Result;

            //Assert.AreEqual(recreated.request.encode(), req.encode());

            //assert.equal(
            //    recreated.chainId.hexString,
            //    "1064487b3cd1a897ce03ae5b6a865651747e2e152090f99c1d19d44e01aea5a4"
            //)

            //var proof = recreated.getIdentityProof(sig)
            
            //assert.equal(
            //    String(proof),
            //    "EOSIO EGRIezzRqJfOA65baoZWUXR+LhUgkPmcHRnUTgGupaQAAAAAAAAoXXQpCF8AAAAAAAAoXQAAAACo7TIyAB9I36p6NdMCKzksNwp4nFbiEhq8/sVAeji/4JMzk/CHAwc5ipaF8G/SNuXkJ9XWaDSu98DWzbXuvaVcimXUvGDQ"
            //)
            //var recreatedProof = IdentityProof.from(String(proof))

            //assert.ok(
            //    recreatedProof.verify(
            //        { threshold= 4, keys=[{ weight= 4, key= key.toPublic()}]},
            //        "2020-07-10T08=00=00"
            //    ),
            //    "verifies valid proof"
            //)

            //assert.ok(
            //    !recreatedProof.verify(
            //        { threshold= 4, keys=[{ weight= 4, key= key.toPublic()}]},
            //        "2020-07-10T09=00=00"
            //    ),
            //    "does not verify expired proof"
            //)
        }

    }
}