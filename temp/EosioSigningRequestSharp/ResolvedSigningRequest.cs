using System;
using System.Threading.Tasks;
using Cryptography.ECDSA;
using EosSharp;
using EosSharp.Core.Api.v1;
using EosSharp.Core.Helpers;
using EosSharp.Core.Providers;

namespace EosioSigningRequest
{
    public class ResolvedSigningRequest
    {
        /** Recreate a resolved request from a callback payload. */
        public static async Task<ResolvedSigningRequest> FromPayload(CallbackPayload payload, SigningRequestEncodingOptions options, IAbiSerializationProvider abiSerializationProvider) {
            var request = SigningRequest.From(payload.Req, options);
            var abis = await request.FetchAbis(abiSerializationProvider);
            return request.Resolve(
                abis,
                new PermissionLevel()
                {
                    actor = payload.Sa,
                    permission = payload.Sp
                },
                new TransactionContext()
                {
                    RefBlockNum = Convert.ToUInt16(payload.Rbn),
                    RefBlockPrefix = Convert.ToUInt32(payload.Rid),
                    Expiration = Convert.ToDateTime(payload.Ex)
                }
            );
        }

        //! The resolved SigningRequest
        public readonly SigningRequest Request;
        //! The signer Permission of the resolved SigningRequest
        public readonly PermissionLevel Signer;
        //! The transaction of the resolved SigningRequest
        public readonly Transaction Transaction;
        // The serializes transaction of the resolved SigningRequest
        public readonly byte[] SerializedTransaction;

        public ResolvedSigningRequest(SigningRequest request, PermissionLevel signer, Transaction transaction, byte[] serializedTransaction)
        {
            Request = request;
            Signer = signer;
            Transaction = transaction;
            SerializedTransaction = serializedTransaction;
        }

        /// <summary>Computes the transactionId of the transaction of the resolved Requestt and returns it as Hex-string.</summary>
        /// <returns>the transactionId</returns>
        public string GetTransactionId()
        {
            return SerializationHelper.ByteArrayToHexString(Sha256Manager.GetHash(SerializedTransaction));
        }

        /// <param name="signatures">Signature applied to payload and callback-url</param>
        /// <param name="blockNum">Blocknum applied to payload and callback-url</param>
        /// <summary>Resolves a Callback</summary>
        /// <returns>ResolvedCallback-Object containing payload, url and metadata</returns>
        public ResolvedCallback GetCallback(string[] signatures, int? blockNum)
        {
            var callback = Request.Data.Callback;
            var flags = Request.Data.Flags;

            if (string.IsNullOrEmpty(callback))
            {
                return null;
            }

            if (signatures == null || signatures.Length == 0)
            {
                throw new Exception("Must have at least one signature to resolve callback");
            }

            var payload = new CallbackPayload()
            {
                Sig = signatures[0],
                Tx = GetTransactionId(),
                Rbn = Transaction.ref_block_num.ToString(),
                Rid = Transaction.ref_block_prefix.ToString(),
                Ex = Transaction.expiration.ToString(),
                Req = Request.Encode(),
                Sa = Signer.actor,
                Sp = Signer.permission,
            };

            if (blockNum != null)
            {
                payload.Bn = blockNum.ToString();
            }

            var url = callback
                .Replace("{{sig}}", payload.Sig)
                .Replace("{{tx}}", payload.Tx)
                .Replace("{{rbn}}", payload.Rbn)
                .Replace("{{rid}}", payload.Rid)
                .Replace("{{ex}}", payload.Ex)
                .Replace("{{req}}", payload.Req)
                .Replace("{{sa}}", payload.Sa)
                .Replace("{{sp}}", payload.Sp)
                .Replace("{{bn}}", payload.Bn);

            return new ResolvedCallback()
            {
                Background = (flags & AbiConstants.RequestFlagsBackground) != 0,
                Payload = payload,
                Url = url
            };
        }
    }
}