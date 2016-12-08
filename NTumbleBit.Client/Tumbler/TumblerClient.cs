﻿using NBitcoin;
using NTumbleBit.ClassicTumbler;
using NTumbleBit.Client.Tumbler.Models;
using NTumbleBit.PuzzlePromise;
using NTumbleBit.PuzzleSolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace NTumbleBit.Client.Tumbler
{
    public class TumblerClient
    {
		public TumblerClient(Network network, Uri serverAddress)
		{
			if(serverAddress == null)
				throw new ArgumentNullException("serverAddress");
			if(network == null)
				throw new ArgumentNullException("network");
			_Address = serverAddress;
			_Network = network;
		}


		private readonly Network _Network;
		public Network Network
		{
			get
			{
				return _Network;
			}
		}


		private readonly Uri _Address;
		public Uri Address
		{
			get
			{
				return _Address;
			}
		}

		readonly static HttpClient Client = new HttpClient();
		public Task<ClassicTumblerParameters> GetTumblerParametersAsync()
		{
			return GetAsync<ClassicTumblerParameters>("api/v1/tumblers/0/parameters");
		}
		public ClassicTumblerParameters GetTumblerParameters()
		{
			return GetTumblerParametersAsync().GetAwaiter().GetResult();
		}

		Task<T> GetAsync<T>(string relativePath, params object[] parameters)
		{
			return SendAsync<T>(HttpMethod.Get, null, relativePath, parameters);
		}

		public AskVoucherResponse AskUnsignedVoucher()
		{
			return AskUnsignedVoucherAsync().GetAwaiter().GetResult();
		}

		public Task<AskVoucherResponse> AskUnsignedVoucherAsync()
		{
			return GetAsync<AskVoucherResponse>("api/v1/tumblers/0/vouchers/");
		}


		public Task<PuzzleSolution> SolveVoucherAsync(uint256 txId)
		{
			return SendAsync<PuzzleSolution>(HttpMethod.Post, txId, "api/v1/tumblers/0/clientchannels/voucher");
		}
		public PuzzleSolution SolveVoucher(uint256 txId)
		{
			return SolveVoucherAsync(txId).GetAwaiter().GetResult();
		}

		public Task<TumblerEscrowInformation> OpenChannelAsync(BobEscrowInformation request)
		{
			if(request == null)
				throw new ArgumentNullException("request");
			return SendAsync<TumblerEscrowInformation>(HttpMethod.Post, request, "api/v1/tumblers/0/channels/");
		}

		public TumblerEscrowInformation OpenChannel(BobEscrowInformation request)
		{
			return OpenChannelAsync(request).GetAwaiter().GetResult();
		}

		public Task<PubKey> RequestTumblerEscrowKeyAsync(ClientEscrowInformation clientEscrowInformation)
		{
			return SendAsync<PubKey>(HttpMethod.Post, clientEscrowInformation, "api/v1/tumblers/0/clientchannels/");
		}
		public PubKey RequestTumblerEscrowKey(ClientEscrowInformation clientEscrowInformation)
		{
			return RequestTumblerEscrowKeyAsync(clientEscrowInformation).GetAwaiter().GetResult();
		}

		private string GetFullUri(string relativePath, params object[] parameters)
		{
			relativePath = String.Format(relativePath, parameters ?? new object[0]);
			var uri = Address.AbsoluteUri;
			if(!uri.EndsWith("/"))
				uri += "/";
			uri += relativePath;
			return uri;
		}

		async Task<T> SendAsync<T>(HttpMethod method, object body, string relativePath, params object[] parameters)
		{
			var uri = GetFullUri(relativePath, parameters);
			var message = new HttpRequestMessage(method, uri);
			if(body != null)
			{
				message.Content = new StringContent(Serializer.ToString(body, Network), Encoding.UTF8, "application/json");
			}
			var result = await Client.SendAsync(message).ConfigureAwait(false);
			if(result.StatusCode == HttpStatusCode.NotFound)
				return default(T);
			if(!result.IsSuccessStatusCode)
			{
				//string error = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
				//if(!string.IsNullOrEmpty(error))
				//{
				//	try
				//	{
				//		var errorObject = Serializer.ToObject<QBitNinjaError>(error, Network);
				//		if(errorObject.StatusCode != 0)
				//			throw new QBitNinjaException(errorObject);
				//	}
				//	catch(JsonSerializationException)
				//	{
				//	}
				//	catch(JsonReaderException)
				//	{
				//	}
				//}
			}
			result.EnsureSuccessStatusCode();
			if(typeof(T) == typeof(byte[]))
				return (T)(object)await result.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
			var str = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
			if(typeof(T) == typeof(string))
				return (T)(object)str;
			return Serializer.ToObject<T>(str, Network);
		}

		public object CheckRevelation(object id, PuzzlePromise.ClientRevelation revelation)
		{
			throw new NotImplementedException();
		}

		public ServerCommitmentsProof CheckRevelation(Script channelId, PuzzlePromise.ClientRevelation revelation)
		{
			return CheckRevelationAsync(channelId, revelation).GetAwaiter().GetResult();
		}

		private Task<ServerCommitmentsProof> CheckRevelationAsync(Script channelId, PuzzlePromise.ClientRevelation revelation)
		{
			return this.SendAsync<ServerCommitmentsProof>(HttpMethod.Post, revelation, "api/v1/tumblers/0/channels/{0}/checkrevelation", channelId.ToHex());
		}

		public Task<PuzzlePromise.ServerCommitment[]> SignHashesAsync(Script channelId, SignaturesRequest sigReq)
		{
			return this.SendAsync<PuzzlePromise.ServerCommitment[]>(HttpMethod.Post, sigReq, "api/v1/tumblers/0/channels/{0}/signhashes", channelId.ToHex());
		}

		public object CheckRevelation(object id, PuzzleSolver.ClientRevelation revelation2)
		{
			throw new NotImplementedException();
		}

		public PuzzleSolver.SolutionKey[] CheckRevelation(Script channelId, PuzzleSolver.ClientRevelation revelation)
		{
			return CheckRevelationAsync(channelId, revelation).GetAwaiter().GetResult();
		}
		public Task<PuzzleSolver.SolutionKey[]> CheckRevelationAsync(Script channelId, PuzzleSolver.ClientRevelation revelation)
		{
			return this.SendAsync<PuzzleSolver.SolutionKey[]>(HttpMethod.Post, revelation, "api/v1/tumblers/0/clientschannels/{0}/checkrevelation", channelId.ToHex());
		}

		public void CheckBlindFactors(Script channelId, BlindFactor[] blindFactors)
		{
			CheckBlindFactorsAsync(channelId, blindFactors).GetAwaiter().GetResult();
		}

		public Task CheckBlindFactorsAsync(Script channelId, BlindFactor[] blindFactors)
		{
			return SendAsync<bool>(HttpMethod.Post, blindFactors, "api/v1/tumblers/0/clientschannels/{0}/checkblindfactors", channelId.ToHex());
		}

		public PuzzleSolver.ServerCommitment[] SolvePuzzles(Script channelId, PuzzleValue[] puzzles)
		{
			return SolvePuzzlesAsync(channelId, puzzles).GetAwaiter().GetResult();
		}

		public Task<PuzzleSolver.ServerCommitment[]> SolvePuzzlesAsync(Script channelId, PuzzleValue[] puzzles)
		{
			return SendAsync<PuzzleSolver.ServerCommitment[]>(HttpMethod.Post, puzzles, "api/v1/tumblers/0/clientchannels/{0}/solvepuzzles", channelId.ToHex());
		}



		public PuzzlePromise.ServerCommitment[] SignHashes(Script channelId, SignaturesRequest sigReq)
		{
			return SignHashesAsync(channelId, sigReq).GetAwaiter().GetResult();
		}
	}
}
