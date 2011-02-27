using System;
using System.IO;

using Manos;

namespace ManosSmoothie {
  public class StaticContentModule : ManosModule {
    public StaticContentModule () {
      Get (".*", Content);
    }

    public static void Content (IManosContext ctx) {
      var path = ctx.Request.Path;
      if (path.StartsWith ("/"))
	    path = path.Substring (1);

      if (File.Exists (path)) {
        ctx.Response.Headers.SetNormalizedHeader ("Content-Type", ManosMimeTypes.GetMimeType (path));
        ctx.Response.SendFile (path);
      } else 
        ctx.Response.StatusCode = 404;

      ctx.Response.End ();
    }
  }
}
