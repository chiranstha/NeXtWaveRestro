using System.Collections.Generic;

namespace NextWave.Erp.Web.Common;

public static class WebConsts
{
    public const string SwaggerUiEndPoint = "/swagger";
    public const string HangfireDashboardEndPoint = "/hangfire";

    public static bool SwaggerUiEnabled = true;
    public static bool HangfireDashboardEnabled = false;

    public static class GraphQL
    {
        public const string EndPoint = "/graphql";
        public const string PlaygroundEndPoint = "/ui/playground";

        public static bool Enabled = true;
        public static bool PlaygroundEnabled = true;
    }

    public static List<string> ReCaptchaIgnoreWhiteList = new List<string>
        {
            ErpConsts.AbpApiClientUserAgent
        };
}

