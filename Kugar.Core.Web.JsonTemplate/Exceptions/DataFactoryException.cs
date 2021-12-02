using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Kugar.Core.Web.JsonTemplate.Exceptions
{
    /// <summary>
    /// 数据获取过程中出现的错误
    /// </summary>
    public class DataFactoryException:Exception
    {
        public DataFactoryException(string? message):base(message){}

        public DataFactoryException(string? message, Exception? innerException):this(message,innerException,null){}

        public DataFactoryException(string? message, Exception? innerException, IJsonTemplateBuilderContext context) :
            base(message, innerException)
        {
            this.Context = context;
        }

        protected DataFactoryException(SerializationInfo info, StreamingContext context):base(info, context)
        {
        }

        public IJsonTemplateBuilderContext Context { get; }
    }

    /// <summary>
    /// 输出过程中,出现的错误
    /// </summary>
    public class OutputRenderException: Exception
    {
        public OutputRenderException(IJsonTemplateBuilderContext context,string message="", Exception? innerException=null) :
            base(message, innerException)
        {
            this.Context = context;
        }

        public string DisplayPropertyPath { set; get; }

        public IJsonTemplateBuilderContext Context { get; }
    }
}
