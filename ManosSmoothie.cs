using Manos;
using Manos.Http;

using System;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace ManosSmoothie {
  public class ManosSmoothie : ManosApp {
    public ManosSmoothie () {
      Route ("/Content/", new StaticContentModule());

      Route ("/processes", ctx => {
        var processlist = Process.GetProcesses();
        ctx.Response.Write(processlist.Length.ToString());
        ctx.Response.End();
      });

      Route ("/", ctx => {
        ctx.Response.SendFile("Templates/index.html");
        ctx.Response.End();
      });
    }
  }
}
