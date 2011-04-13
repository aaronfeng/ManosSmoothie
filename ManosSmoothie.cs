using Manos;
using Manos.Http;
using Manos.Ws;

using System;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace ManosSmoothie {

	public class ManosSmoothie : ManosApp {
		public ManosSmoothie () {
			Get ("/Content/", new StaticContentModule());

			Get ("/", ctx => {
				ctx.Response.SendFile("Templates/index.html");
				ctx.Response.End();
			});
		}

		public void ProcessCount (IManosContext ctx)
		{
			WebSocket ws = WebSocket.Upgrade (ctx.Request);

			Random rand = new Random ();

			var t = AddTimeout (TimeSpan.FromSeconds (0.5), RepeatBehavior.Forever, (app, data) => {
				int r = rand.Next (10, 100);
				ws.Send (r.ToString ());
			});

			ws.Closed += delegate {
				t.Stop ();
			};
		}
	}
}


