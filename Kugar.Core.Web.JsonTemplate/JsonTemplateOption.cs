using System;
using System.Collections.Generic;
using System.Text;

namespace Kugar.Core.Web.JsonTemplate
{
    public enum NullObjectFormattingEnum
    {
        EmptyObject,

        Null
    }

    public enum NullArrayFormattingEnum
    {
        EmptyArray,

        Null
    }

    public class JsonTemplateOption
    {
        public NullObjectFormattingEnum NullObjectFormatting { set; get; } = NullObjectFormattingEnum.Null;

        public NullArrayFormattingEnum NullArrayFormatting { set; get; } = NullArrayFormattingEnum.Null;


    }
}
