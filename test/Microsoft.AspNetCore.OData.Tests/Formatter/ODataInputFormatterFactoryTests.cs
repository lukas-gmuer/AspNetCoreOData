// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Formatter.Deserialization;
using Microsoft.AspNetCore.OData.Tests.Commons;
using Microsoft.AspNetCore.OData.Tests.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;
using Xunit;

namespace Microsoft.AspNetCore.OData.Tests.Formatter
{
    public class ODataInputFormatterFactoryTests
    {
        private static IServiceProvider _serviceProvider = BuildServiceProvider();
        private static IEdmModel _edmModel = GetEdmModel();
        private static IList<ODataInputFormatter> _formatters = ODataInputFormatterFactory.Create();

        [Fact]
        public void CreateReturnsCorrectInputFormattersCount()
        {
            // Arrange & Act & Assert
            Assert.Equal(3, _formatters.Count);
        }

        [Fact]
        public void ODataInputFormattersContainsSupportedMediaTypes()
        {
            // Arrange
            string[] expectedMediaTypes = new string[]
            {
                "application/json;odata.metadata=minimal;odata.streaming=true",
                "application/json;odata.metadata=minimal;odata.streaming=false",
                "application/json;odata.metadata=minimal",
                "application/json;odata.metadata=full;odata.streaming=true",
                "application/json;odata.metadata=full;odata.streaming=false",
                "application/json;odata.metadata=full",
                "application/json;odata.metadata=none;odata.streaming=true",
                "application/json;odata.metadata=none;odata.streaming=false",
                "application/json;odata.metadata=none",
                "application/json;odata.streaming=true",
                "application/json;odata.streaming=false",
                "application/json",
                "application/xml"
            };

            // Act
            IEnumerable<string> supportedMediaTypes = _formatters.SelectMany(f => f.SupportedMediaTypes).Distinct();

            // Assert
            Assert.True(expectedMediaTypes.SequenceEqual(supportedMediaTypes));
        }

        [Fact]
        public void ODataInputFormattersContainsSupportedEncodings()
        {
            // Arrange
            string[] expectedEncodings = new string[]
            {
                "Unicode (UTF-8)",
                "Unicode"
            };

            // Act
            IEnumerable<string> supportedEncodings = _formatters.SelectMany(f => f.SupportedEncodings).Distinct().Select(c => c.EncodingName);

            // Assert
            Assert.True(expectedEncodings.SequenceEqual(supportedEncodings));
        }

        public static TheoryDataSet<Type, string[]> InputFormatterSupportedMediaTypesTests
        {
            get
            {
                string[] applicationJsonMediaTypes = new[]
                {
                    "application/json;odata.metadata=minimal;odata.streaming=true",
                    "application/json;odata.metadata=minimal;odata.streaming=false",
                    "application/json;odata.metadata=minimal",
                    "application/json;odata.metadata=full;odata.streaming=true",
                    "application/json;odata.metadata=full;odata.streaming=false",
                    "application/json;odata.metadata=full",
                    "application/json;odata.metadata=none;odata.streaming=true",
                    "application/json;odata.metadata=none;odata.streaming=false",
                    "application/json;odata.metadata=none",
                    "application/json;odata.streaming=true",
                    "application/json;odata.streaming=false",
                    "application/json",
                };

                return new TheoryDataSet<Type, string[]>
                {
                    { typeof(SampleType), applicationJsonMediaTypes },
                    { typeof(Uri), applicationJsonMediaTypes },
                    { typeof(ODataActionParameters), applicationJsonMediaTypes }
                };
            }
        }

        [Theory]
        [MemberData(nameof(InputFormatterSupportedMediaTypesTests))]
        public void ODataInputFormattersForReadTypeReturnsSupportedMediaTypes(Type type, string[] expectedMediaTypes)
        {
            // Arrange
            HttpRequest request = RequestFactory.Create(f => { f.Model = _edmModel; f.Path = new ODataPath(); });
            request.HttpContext.RequestServices = _serviceProvider;
            var metadataDocumentFormatters = _formatters.Where(f => CanReadType(f, type, request));

            // Act
            IEnumerable<string> supportedMediaTypes = metadataDocumentFormatters.SelectMany(f => f.SupportedMediaTypes).Distinct();

            // Assert
            Assert.True(expectedMediaTypes.SequenceEqual(supportedMediaTypes));
        }

        private static IServiceProvider BuildServiceProvider()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddSingleton<ODataDeserializerProvider, DefaultODataDeserializerProvider>();

            // Deserializers.
            services.AddSingleton<ODataResourceDeserializer>();
            services.AddSingleton<ODataEnumDeserializer>();
            services.AddSingleton<ODataPrimitiveDeserializer>();
            services.AddSingleton<ODataResourceSetDeserializer>();
            services.AddSingleton<ODataCollectionDeserializer>();
            services.AddSingleton<ODataEntityReferenceLinkDeserializer>();
            services.AddSingleton<ODataActionPayloadDeserializer>();
            return services.BuildServiceProvider();
        }

        private static IEdmModel GetEdmModel()
        {
            ODataConventionModelBuilder model = new ODataConventionModelBuilder();
            model.EntityType<SampleType>();
            return model.GetEdmModel();
        }

        private static bool CanReadType(ODataInputFormatter formatter, Type type, HttpRequest request)
        {
            var context = new InputFormatterContext(
                request.HttpContext,
                "modelName",
                new ModelStateDictionary(),
                new EmptyModelMetadataProvider().GetMetadataForType(type),
                (stream, encoding) => new StreamReader(stream, encoding));

            return formatter.CanRead(context);
        }

        private class SampleType
        {
            public int Id { get; set; }
        }
    }
}