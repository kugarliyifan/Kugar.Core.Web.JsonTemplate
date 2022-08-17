using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Text;
using System.Xml.Linq;
using Kugar.Core.Web.JsonTemplate.Helpers;
using Kugar.Core.Web.JsonTemplate.Invokers;
using NJsonSchema;
using NJsonSchema.Generation;

namespace Kugar.Core.Web.JsonTemplate.Builders
{
    public class RootObjectTemplateBuilder<TModel>: IObjectBuilderPipe<TModel,TModel>, IObjectBuilderInfo
    {
        public RootObjectTemplateBuilder(
            NSwagSchemeBuilder schemeBuilder,
            JsonSchemaGenerator generator,
            JsonSchemaResolver resolver)
        {
            this.Generator = generator;
            this.SchemaBuilder = schemeBuilder;
            this.Resolver = resolver;

        }

        public virtual void Start()
        {
            Pipe.Add((writer, context) =>
            {
                writer.WriteStartObject();
            });
             
        }

        public virtual void End()
        {
            Pipe.Add((writer, context) =>
            {
                writer.WriteEndObject();
            });

        }

        public IList<PipeActionBuilder<TModel, TModel>> Pipe { get; } = new List<PipeActionBuilder<TModel, TModel>>();

        public NSwagSchemeBuilder SchemaBuilder { get; }

        public JsonSchemaGenerator Generator { get; }

        public JsonSchemaResolver Resolver { get; }

        public virtual Type ModelType { get; } = typeof(TModel);

        public RootObjectTemplateBuilder<TModel> AddProperty<TValue>(string propertyName, Func<IJsonTemplateBuilderContext<TModel,TModel>, TValue> valueFactory, string description = "",
            bool isNull = false, object example = null, Type newValueType = null, Func<IJsonTemplateBuilderContext<TModel,TModel>, bool> ifCheckExp = null)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(propertyName));
            Debug.Assert(valueFactory != null);

            propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);

            var propertyInvoke = new PropertyInvoker<TModel, TModel, TValue>()
            {
                ifCheckExp = ifCheckExp,
                ParentDisplayName = propertyName,
                PropertyName = propertyName,
                valueFactory = valueFactory
            };

            Pipe.Add(propertyInvoke.Invoke);

            JsonObjectType jsonType = NSwagSchemeBuilder.NetTypeToJsonObjectType(newValueType ?? typeof(TValue));


            SchemaBuilder.AddSingleProperty(propertyName, jsonType,
                description, example, isNull);

            return this;
        }

        public virtual ITemplateBuilder<TModel,TChildModel> AddObject<TChildModel>(string propertyName,
            Func<IJsonTemplateBuilderContext<TModel, TModel>, TChildModel> valueFactory,
            bool isNull = false,
            string description = "",
            Func<IJsonTemplateBuilderContext<TModel, TChildModel>, bool> ifCheck = null)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(propertyName));
            Debug.Assert(valueFactory != null);

            propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);

            //SchemaBuilder.AddObjectProperty(propertyName, description, isNull);

            //_pipe.Add(async (writer, context) =>
            //{
            //    await writer.WritePropertyNameAsync(propertyName, context.CancellationToken);
            //});

            var childSchemeBuilder = SchemaBuilder.AddObjectProperty(propertyName, description, isNull);

            return new ChildJsonTemplateObjectBuilder<TModel, TModel, TChildModel>(
                propertyName,
                propertyName,
                this,
                valueFactory,
                childSchemeBuilder,
                Generator,
                Resolver,
                true, ifCheck).Start();
        }

        public virtual SameRootTemplateBuilder<TModel> AddObject(string propertyName,
            Func<IJsonTemplateBuilderContext<TModel, TModel>, TModel> valueFactory,
            bool isNull = false,
            string description = "",
            Func<IJsonTemplateBuilderContext<TModel, TModel>, bool> ifCheck = null)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(propertyName));
            Debug.Assert(valueFactory != null);

            propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);

            //SchemaBuilder.AddObjectProperty(propertyName, description, isNull);

            //_pipe.Add(async (writer, context) =>
            //{
            //    await writer.WritePropertyNameAsync(propertyName, context.CancellationToken);
            //});

            var childSchemeBuilder = SchemaBuilder.AddObjectProperty(propertyName, description, isNull);

            var t = new SameRootTemplateBuilder<TModel>(
                propertyName,
                propertyName,
                this,
                childSchemeBuilder,
                Generator,
                Resolver,
                true, ifCheck);

            t.Start();

            return t;

        }

        public virtual IArrayBuilder<TModel, TModel, TArrayNewElement> AddArrayObject<TArrayNewElement>(
            string propertyName,
            Func<IJsonTemplateBuilderContext<TModel, TModel>, IEnumerable<TArrayNewElement>> valueFactory,
            bool isNull = false,
            string description = "",
            Func<IJsonTemplateBuilderContext<TModel, TArrayNewElement>, bool> ifCheckExp = null)
        {
            propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);

            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                Pipe.Add((writer, model) =>
                {
                    writer.WritePropertyName(propertyName);
                });
            }
            else
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            var s1 = SchemaBuilder.AddObjectArrayProperty(propertyName, desciption: description, nullable: isNull);

            var s = new ArrayObjectTemplateObjectBuilder<TModel, TModel, TArrayNewElement>(propertyName, propertyName, this, valueFactory, s1, Generator, Resolver, ifCheckExp);

            return s;
        }

        public virtual ISameRootArrayBuilder<TArrayNewElement> AddLiteArrayObject<TModel, TArrayNewElement>(
            string propertyName,
            Func<IJsonTemplateBuilderContext<TModel, TModel>, IEnumerable<TArrayNewElement>> valueFactory,
            bool isNull = false,
            string description = "",
            Func<IJsonTemplateBuilderContext<TModel, TArrayNewElement>, bool> ifCheckExp = null)
        {
            propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);

            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                Pipe.Add((writer, model) =>
                {
                    writer.WritePropertyName(propertyName);
                });
            }
            else
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            var s1 = SchemaBuilder.AddObjectArrayProperty(propertyName, desciption: description, nullable: isNull);

            var s = new SameRootArrayBuilder<TArrayNewElement>(propertyName, 
                propertyName,
                (IObjectBuilderPipe<IEnumerable<TArrayNewElement>, IEnumerable<TArrayNewElement>>)this,
                s1, 
                Generator, 
                Resolver,
                (t)=>ifCheckExp?.Invoke(new JsonTemplateBuilderContext<TModel, TArrayNewElement>(t.HttpContext,(TModel)t.RootModel,t.Model,t.JsonSerializerSettings,new Lazy<TemplateData>(t.GlobalTemporaryData)))??true);

            return s;
        }


        public virtual RootObjectTemplateBuilder<TModel> AddArrayValue<TValue>(string propertyName, Func<IJsonTemplateBuilderContext<TModel, TModel>, IEnumerable<TValue>> valueFactory, bool isNull = false,
            string description = "", Func<IJsonTemplateBuilderContext<TModel, IEnumerable<TValue>>, bool> ifNullRender = null)
        {
            propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);

            var propertyInvoke = new ArrayValueInvoker<TModel, TModel, TValue>()
            {
                ifNullRender = ifNullRender,
                ParentDisplayName = propertyName,
                PropertyName = propertyName,
                valueFactory = valueFactory
            };

            Pipe.Add(propertyInvoke.Invoke);

            JsonObjectType jsonType = NSwagSchemeBuilder.NetTypeToJsonObjectType(typeof(TValue));


            var s1 = SchemaBuilder.AddValueArray(propertyName, jsonType, desciption: description, nullable: isNull);

            return this;
        }

        public void Dispose()
        {
            this.End();
        }
    }

    public class  RootArrayObjectTemplateBuilder<TArrayElement> : RootObjectTemplateBuilder<IEnumerable<TArrayElement>>
    {
        private List<PipeActionBuilder<IEnumerable<TArrayElement>, IEnumerable<TArrayElement>>> _pipe = new List<PipeActionBuilder<IEnumerable<TArrayElement>, IEnumerable<TArrayElement>>>();
        
        public virtual SameRootArrayBuilder<TArrayElement> AddArrayObject(
            string propertyName,
            Func<IJsonTemplateBuilderContext<IEnumerable<TArrayElement>, IEnumerable<TArrayElement>>, IEnumerable<TArrayElement>> valueFactory,
            bool isNull = false,
            string description = "",
            Func<IJsonTemplateBuilderContext<IEnumerable<TArrayElement>, TArrayElement>, bool> ifCheckExp = null)
        {
            propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);

            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                Pipe.Add((writer, model) =>
                {
                    writer.WritePropertyName(propertyName);
                });
            }
            else
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            var s1 = SchemaBuilder.AddObjectArrayProperty(propertyName, desciption: description, nullable: isNull);

            var s = new SameRootArrayBuilder<TArrayElement>(propertyName, propertyName, this, s1, Generator, Resolver, ifCheckExp);

            return s;
        }

        public override void Start()
        {
            Pipe.Add((writer, context) =>
            {
                writer.WriteStartArray();
            }); 
        }

        public override void End()
        {
            Pipe.Add((writer, context) =>
            {
                writer.WriteEndArray();
            });

        }
         
        public override Type ModelType { get; } = typeof(IEnumerable<TArrayElement>);

        public RootArrayObjectTemplateBuilder(NSwagSchemeBuilder schemeBuilder, JsonSchemaGenerator generator, JsonSchemaResolver resolver) : base(schemeBuilder, generator, resolver)
        {
        }
    }

    public class SameRootTemplateBuilder<TRootModel> : ChildJsonTemplateObjectBuilder<TRootModel, TRootModel, TRootModel>
    {
 

        public override void Dispose()
        {
            End();
        }

        public SameRootTemplateBuilder(string propertyName, 
            string displayPropertyName, 
            IObjectBuilderPipe<TRootModel, TRootModel> parent,  
            NSwagSchemeBuilder schemeBuilder,
            JsonSchemaGenerator generator,
            JsonSchemaResolver resolver, 
            bool isNewObject, 
            Func<IJsonTemplateBuilderContext<TRootModel, TRootModel>, bool> ifCheckExp = null) : 
            base(propertyName, displayPropertyName, parent, (c)=>c.Model, schemeBuilder, generator, resolver, isNewObject, ifCheckExp)
        {
        }
    }

    public interface
        ISameRootArrayBuilder<TElementModel> : IArrayBuilder<IEnumerable<TElementModel>, IEnumerable<TElementModel>,
            TElementModel>
    {

    }

    public class  SameRootArrayBuilder<TElementModel> : ArrayObjectTemplateObjectBuilder<IEnumerable<TElementModel>, IEnumerable<TElementModel>, TElementModel>  , ISameRootArrayBuilder<TElementModel>
    {
        public SameRootArrayBuilder(string properyName, 
            string displayPropertyName, 
            IObjectBuilderPipe<IEnumerable<TElementModel>, IEnumerable<TElementModel>> parent,  
            NSwagSchemeBuilder schemeBuilder, 
            JsonSchemaGenerator generator, 
            JsonSchemaResolver resolver, 
            Func<IJsonTemplateBuilderContext<IEnumerable<TElementModel>, TElementModel>, bool> ifCheckExp = null) 
            : base(properyName, displayPropertyName, parent, (c)=>c.Model, schemeBuilder, generator, resolver, ifCheckExp)
        {
        }
    }
}
