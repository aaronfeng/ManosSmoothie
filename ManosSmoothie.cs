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
      Route ("/Content/", new StaticContentModule());

      Route ("/", ctx => {
        ctx.Response.SendFile("Templates/index.html");
        ctx.Response.End();
      });
    }

	  public void ProcessCount (IManosContext ctx)
	  {
		  WebSocket ws = WebSocket.Upgrade (ctx.Request);

		  var t = AddTimeout (TimeSpan.FromSeconds (3), RepeatBehavior.Forever, (app, data) => {
			  var processlist = Process.GetProcesses();
			  ws.Send (processlist.Length.ToString());
		  });

		  ws.Closed += delegate {
			  t.Stop ();
		  };
	  }
  }
}
