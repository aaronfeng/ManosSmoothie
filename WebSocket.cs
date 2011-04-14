//
// Copyright (C) 2010 Jackson Harper (jackson@manosdemono.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
//

using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

using Manos;
using Manos.IO;
using Manos.IO.Libev;
using Manos.Http;


namespace Manos.Ws {


	public class WebSocket {

		private static readonly byte [] MESSAGE_START = new byte [] { 0x00 };
		private static readonly byte [] MESSAGE_END = new byte [] { 0xFF };
		
		private ISocketStream socket;

		internal WebSocket (ISocketStream socket)
		{
			this.socket = socket;

			socket.Closed += delegate {
				if (Closed != null)
					Closed (this, EventArgs.Empty);
				
			};
		}

		
		// We can do all this stuff without a callback because it doesnt really matter if the
		// handshake isn't complete before we start writing, since everything is queued.
		public static WebSocket Upgrade (IHttpRequest request)
		{
			if (request == null)
				throw new ArgumentNullException ("request");

			if (request.Method != HttpMethod.HTTP_GET)
				throw new InvalidOperationException ("Only GET operations can be upgraded.");

			string upgrade;
			if (!request.Headers.TryGetValue ("Upgrade", out upgrade) || upgrade != "WebSocket")
				throw new InvalidOperationException ("WebSockets require the Upgrade header set to WebSocket.");

			string connection;
			if (!request.Headers.TryGetValue ("Connection", out connection) || connection != "Upgrade")
				throw new InvalidOperationException ("WebSockets require the Connection header set to Upgrade.");

			
			// So really we should be reading the body data here instead of the UPGRADE_HEAD hack
			// request.Socket.ReadBytes (...

			WebSocket res = new WebSocket (request.Socket);

			res.SendHandshake (request);
			return res;
		}

		public void Send (string data)
		{
			Send (Encoding.Default.GetBytes (data));
		}

		public void Send (string data, WriteCallback cb)
		{
			Send (Encoding.Default.GetBytes (data), cb);
		}

		public void Send (byte [] data)
		{
			Send (data, null);
		}

		public void Send (byte [] data, WriteCallback cb)
		{
			socket.Write (MESSAGE_START, null);
			socket.Write (data, null);
			socket.Write (MESSAGE_END, cb);
		}

		private void SendHandshake (IHttpRequest request)
		{
			var url = request.Path;
			var hs = new StringBuilder ();
			int version = DetermineVersion (request);

			hs.Append ("HTTP/1.1 101 Web Socket Protocol Handshake\r\n");
			hs.Append ("Upgrade: WebSocket\r\n");
			hs.Append ("Connection: Upgrade\r\n");

			string head_prepend = String.Empty;
			if (version == 76)
				head_prepend = "Sec-";
			hs.AppendFormat ("{0}WebSocket-Origin: {1}\r\n", head_prepend, request.Headers ["Origin"]);
			hs.AppendFormat ("{0}WebSocket-Location: ws://{1}{2}\r\n", head_prepend, request.Headers ["Host"], url);
			hs.Append ("\r\n");

			socket.Write (Encoding.ASCII.GetBytes (hs.ToString ()), null);

			if (version == 76) {
				string key1 = request.Headers ["sec-websocket-key1"];
				string key2 = request.Headers ["sec-websocket-key2"];
				byte [] body = (byte []) request.GetProperty ("UPGRADE_HEAD");

				socket.Write (GetSecurityKey (key1, key2, body), null);
			}
		}

		public static int DetermineVersion (IHttpRequest request)
		{
			string dummy;
			return request.Headers.TryGetValue ("sec-websocket-key1", out dummy) && request.Headers.TryGetValue ("sec-websocket-key2", out dummy) ? 76 : 75;
		}


		/*
		 * Adapted from: http://superwebsocket.codeplex.com/
		 * Available under the BSD license
		 * Copyright 2010 Kerry Jiang (kerry-jiang@hotmail.com)
		 */
		public static byte [] GetSecurityKey (string secKey1, string secKey2, byte [] body)
		{
			// Remove all symbols that are not numbers
			string k1 = Regex.Replace (secKey1, "[^0-9]", String.Empty);
			string k2 = Regex.Replace (secKey2, "[^0-9]", String.Empty);

			// Convert received string to 64 bit integer.
			Int64 intK1 = Int64.Parse (k1);
			Int64 intK2 = Int64.Parse (k2);

			//Dividing on number of spaces	
			int k1Spaces = secKey1.Count(c => c == ' ');
			int k2Spaces = secKey2.Count(c => c == ' ');
			int k1FinalNum = (int)(intK1 / k1Spaces);
			int k2FinalNum = (int)(intK2 / k2Spaces);

			//Getting byte parts	
			byte[] b1 = BitConverter.GetBytes(k1FinalNum).Reverse().ToArray();
			byte[] b2 = BitConverter.GetBytes(k2FinalNum).Reverse().ToArray();

			//Concatenating everything into 1 byte array for hashing.	
			List<byte> bChallenge = new List<byte>();
			bChallenge.AddRange(b1);
			bChallenge.AddRange(b2);
			bChallenge.AddRange (body);

			//Hash and return	
			byte[] hash = MD5.Create().ComputeHash(bChallenge.ToArray());

			return hash;
		}

		public event EventHandler Closed;
	}
}

