using System;
using System.Collections.Generic;
using Kugar.Core.Web.JsonTemplate.Builders;

namespace Kugar.Core.Web.JsonTemplate
{
    public interface IJsonTemplateObject 
    { 
        Type ModelType { get; }
        
    }




    public interface IJsonTemplateBase<TModel>: IJsonTemplateObject
    {
        void BuildScheme(RootObjectTemplateBuilder<TModel> builder);
    }

    public abstract class JsonEmptyTemplateBase<TModel> : IJsonTemplateBase<TModel>
    {
        public virtual Type ModelType { get; }

        /// <summary>
        /// 构建属性的输出,builder一定不要使用using
        /// </summary>
        /// <param name="builder">属性构建器,,一定不要对builder使用using</param>
        public abstract void BuildScheme(RootObjectTemplateBuilder<TModel> builder);
    }

    /// <summary>
    /// 用于构建输出模板的基类
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    public abstract class JsonTemplateBase<TModel> :  IJsonTemplateBase<TModel>
    {
        /// <summary>
        /// 构建属性的输出,builder一定不要使用using
        /// </summary>
        /// <param name="builder">属性构建器,,一定不要对builder使用using</param>
        public abstract  void BuildScheme(RootObjectTemplateBuilder<TModel> builder); 

        /// <summary>
        /// 当前的模型类型
        /// </summary>
        public Type ModelType => typeof(TModel);

        /// <summary>
        /// 检查是否添加该属性,,返回true,输出该属性,,false为不输出该属性
        /// </summary>
        /// <param name="model"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        protected virtual bool PropertyRenderCheck(IJsonTemplateBuilderContext model, string propertyName)
        {
            return true;
        }

        public ITemplateBuilder<TModel, TNewObject> FromObject<TModel, TNewObject>(RootObjectTemplateBuilder<TModel> builder,
            Func<IJsonTemplateBuilderContext<TModel, TModel>, TNewObject> objectFactory
        )
        {
            if (objectFactory == null)
            {
                throw new ArgumentNullException(nameof(objectFactory));
            }

            return new ChildJsonTemplateObjectBuilder<TModel, TModel, TNewObject>("", "", builder,
                objectFactory, builder.SchemaBuilder, builder.Generator, builder.Resolver, isNewObject: false).Start();
        }
    }


    //public abstract class JsonArrayTemplateBase<TArrayElement> : IJsonTemplateBase<IEnumerable<TArrayElement>>
    //{
    //    /// <summary>
    //    /// 构建属性的输出,builder一定不要使用using
    //    /// </summary>
    //    /// <param name="builder">属性构建器,,一定不要对builder使用using</param>
    //    public abstract void BuildScheme(RootObjectTemplateBuilder<IEnumerable<TArrayElement>> builder);
         

    //    public Type ModelType { get; } = typeof(IEnumerable<TArrayElement>);

    //    public ITemplateBuilder<IEnumerable<TArrayElement>, TNewObject> FromObject<TNewObject>(RootArrayObjectTemplateBuilder<TArrayElement> builder,
    //        Func<IJsonTemplateBuilderContext<IEnumerable<TArrayElement>, IEnumerable<TArrayElement>>, TNewObject> objectFactory
    //    )
    //    {
    //        if (objectFactory == null)
    //        {
    //            throw new ArgumentNullException(nameof(objectFactory));
    //        }

    //        return new ChildJsonTemplateObjectBuilder<IEnumerable<TArrayElement>, IEnumerable<TArrayElement>, TNewObject>("", "", builder,
    //            objectFactory, builder.SchemaBuilder, builder.Generator, builder.Resolver, isNewObject: false).Start();
    //    }

    //}


}