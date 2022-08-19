using System;
using System.Collections.Generic;
using System.Text;
using Kugar.Core.Configuration;
using Kugar.Core.ExtMethod;

namespace Kugar.Core.Web.JsonTemplate
{
    public static class GlobalSettings
    {
        static GlobalSettings()
        {
            IsRenderTrace = CustomConfigManager.Default["JsonTemplate:RenderTrace"].ToBool();
#if DEBUG
            IsRenderTrace = true;
#endif

        }

        public static bool IsRenderTrace { get; }
    }
}
