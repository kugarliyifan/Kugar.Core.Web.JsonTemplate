namespace Kugar.Core.Web.JsonTemplate
{
    public interface IJsonTemplateObject 
    { 
    }

    public abstract class JsonTemplateObjectBase<TModel> :IJsonTemplateObject
    {
        public abstract void BuildScheme(IObjectBuilder<TModel> builder);
                
    }
}