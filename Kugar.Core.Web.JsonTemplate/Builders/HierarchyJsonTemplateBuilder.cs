using Kugar.Core.ExtMethod;
using Newtonsoft.Json;
using NJsonSchema;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using NJsonSchema.Generation;

namespace Kugar.Core.Web.JsonTemplate.Builders
{
    public delegate IEnumerable<T> HierarchyFactory<T>(IEnumerable<T> collection, T parentItem);

    public class HierarchyJsonTemplateBuilder<TRootModel, TParentModel, TElementModel> : ArrayObjectTemplateObjectBuilder<TRootModel, TParentModel, TElementModel>
    {
        private List<PipeActionBuilder<TRootModel, TElementModel>> _pipe = new List<PipeActionBuilder<TRootModel, TElementModel>>();
        private Func<IJsonTemplateBuilderContext<TRootModel, TParentModel>, IEnumerable<TElementModel>> _arrayValueFactory = null;
        private IList<PipeActionBuilder<TRootModel, TParentModel>> _parent = null;
        private Func<IJsonTemplateBuilderContext<TRootModel, TElementModel>, bool> _ifCheckExp = null;
        private Func<TElementModel, bool> _rootCheck = null;
        private string _childFieldName = "children";
        private HierarchyFactory<TElementModel> _hierarchyFactory = null;

        public HierarchyJsonTemplateBuilder(
            string propertyName,
            string displayPropertyName,
            ITemplateBuilder<TRootModel, TParentModel> parent,
            [Required] Func<IJsonTemplateBuilderContext<TRootModel, TParentModel>, IEnumerable<TElementModel>> arrayValueFactory,
            NSwagSchemeBuilder schemeBuilder,
            JsonSchemaGenerator generator,
            JsonSchemaResolver resolver,
            HierarchyFactory<TElementModel> hierarchyFactory,
            Func<TElementModel, bool> rootChecker,
            string childFieldName = "children",
            Func<IJsonTemplateBuilderContext<TRootModel, TElementModel>, bool> ifCheckExp = null
        ) : base(propertyName, displayPropertyName, parent, arrayValueFactory, schemeBuilder, generator, resolver, ifCheckExp)
        {
            _parent = parent.Pipe;
            _arrayValueFactory = arrayValueFactory;
            _ifCheckExp = ifCheckExp;
            _rootCheck = rootChecker;
            _childFieldName = childFieldName;
            _hierarchyFactory = hierarchyFactory;
        }

        public override IArrayBuilder<TRootModel, TParentModel, TElementModel> End()
        {
            SchemaBuilder.AddValueArray(_childFieldName, JsonObjectType.Array, desciption: "子级列表,子级内容与父级参数一致", nullable: true);

            _parent.Add(endAction);

            return this;
        }

        public void Dispose()
        {
            this.End();
        }

        public IList<PipeActionBuilder<TRootModel, TElementModel>> Pipe => _pipe;

        public Type ModelType { get; } = typeof(TElementModel);

        public void writeHierarchy(JsonWriter writer,
            string childrenFieldName,
            IEnumerable<TElementModel> elements,
            TElementModel parentElement,
            HierarchyFactory<TElementModel> childFactory,
            JsonTemplateBuilderContext<TRootModel, TElementModel> context
            )
        {
            var clist = childFactory(elements, parentElement).ToArrayEx();

            if (clist.HasData())
            {
                writer.WritePropertyName(childrenFieldName);

                writer.WriteStartArray();

                foreach (var item in clist)
                {
                    writer.WriteStartObject();
                    var newContext = new JsonTemplateBuilderContext<TRootModel, TElementModel>(context.HttpContext,
                        context.RootModel, item, context.JsonSerializerSettings,
                        new Lazy<TemplateData>(context.GlobalTemporaryData));

                    invokePipe(writer, newContext);

                    writeHierarchy(writer, _childFieldName, elements, item, childFactory, context);

                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
            }
        }

        private void invokePipe(JsonWriter writer, JsonTemplateBuilderContext<TRootModel, TElementModel> context)
        {
            foreach (var func in _pipe)
            {
                try
                {
                    func(writer, context);
                }
                catch (Exception e)
                {
                    Debugger.Break();
                    throw;
                }

            }
        }

        private void endAction(JsonWriter writer, IJsonTemplateBuilderContext<TRootModel, TParentModel> context)
        {
            if (!string.IsNullOrEmpty(this.DisplayPropertyName))
            {
                writer.WritePropertyName(this.DisplayPropertyName);
            }

            writer.WriteStartArray();

            var array = _arrayValueFactory(new JsonTemplateBuilderContext<TRootModel, TParentModel>(context.HttpContext, context.RootModel, context.Model, context.JsonSerializerSettings)
            {
                PropertyName = this.DisplayPropertyName
            }
            );

            if (array?.HasData() ?? false)
            {
                var rootList = array.Where(_rootCheck).ToArrayEx();

                foreach (var element in rootList)
                {
                    var newContext = new JsonTemplateBuilderContext<TRootModel, TElementModel>(context.HttpContext, context.RootModel, element, context.JsonSerializerSettings)
                    {
                        //PropertyRenderChecker = context.PropertyRenderChecker
                        PropertyName = this.DisplayPropertyName
                    };

                    if (!(_ifCheckExp?.Invoke(newContext) ?? true))
                    {
                        continue;
                    }

                    writer.WriteStartObject();

                    invokePipe(writer, newContext);

                    //writer.WritePropertyName(_childFieldName);

                    writeHierarchy(writer, _childFieldName, array, element, _hierarchyFactory, newContext);

                    writer.WriteEndObject();
                }
            }

            writer.WriteEndArray();
        }
    }

    public static class HierarchyExtMethod
    {
        public static IArrayBuilder<TRootModel,TParentModel,TElementModel> FromHierarchy<TRootModel, TParentModel, TElementModel>(
            this ITemplateBuilder<TRootModel,IEnumerable<TElementModel>> builder,
            Func<IJsonTemplateBuilderContext<TRootModel, TParentModel>, IEnumerable<TElementModel>> arrayFactory,
            HierarchyFactory<TElementModel> hierarchyFactory,
            Func<TElementModel, bool> rootCheck,
            string childrenFieldName = "children"
        ) //where TParentModel:IEnumerable<TElementModel>
        {
            //var tmp = new PropertyExpInvoker<IEnumerable<TElementModel>, IEnumerable<TElementModel>>("", objectPropertyExp);


            return new HierarchyJsonTemplateBuilder<TRootModel, TParentModel, TElementModel>(
                "",
                "",
                (ITemplateBuilder<TRootModel,TParentModel>)builder,
                arrayFactory,
                builder.SchemaBuilder,
                builder.Generator,
                builder.Resolver,
                hierarchyFactory,
                rootCheck,
                childrenFieldName
            );
        }

        public static IArrayBuilder<TRootModel, TParentModel, TElementModel> AddHierarchy<TRootModel, TParentModel, TElementModel>(
            this ITemplateBuilder<TRootModel, TParentModel> src,
            string propertyName,
            [NotNull] Func<IJsonTemplateBuilderContext<TRootModel, TParentModel>, IEnumerable<TElementModel>> arrayFactory,
            string childrenFieldName,
            HierarchyFactory<TElementModel> hierarchyFactory,
            Func<TElementModel, bool> rootCheck
            ) where TParentModel : IEnumerable<TElementModel>
        {
            return new HierarchyJsonTemplateBuilder<TRootModel, TParentModel, TElementModel>(
                propertyName,
                propertyName,
                src,
                arrayFactory,
                src.SchemaBuilder,
                src.Generator, src.Resolver,
                hierarchyFactory,
                rootCheck,
                childrenFieldName
            );
        }
    }
}
