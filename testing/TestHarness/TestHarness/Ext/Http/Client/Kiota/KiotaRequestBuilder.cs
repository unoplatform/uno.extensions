// <auto-generated/>
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.Kiota.Abstractions;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
using TestHarness.Ext.Http.Kiota.Client.Kiota.Data;
using TestHarness.Ext.Http.Kiota.Client.Kiota.Login;
namespace TestHarness.Ext.Http.Kiota.Client.Kiota
{
    /// <summary>
    /// Builds and executes requests for operations under \Kiota
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.16.0")]
    public partial class KiotaRequestBuilder : BaseRequestBuilder
    {
        /// <summary>The data property</summary>
        public global::TestHarness.Ext.Http.Kiota.Client.Kiota.Data.DataRequestBuilder Data
        {
            get => new global::TestHarness.Ext.Http.Kiota.Client.Kiota.Data.DataRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>The login property</summary>
        public global::TestHarness.Ext.Http.Kiota.Client.Kiota.Login.LoginRequestBuilder Login
        {
            get => new global::TestHarness.Ext.Http.Kiota.Client.Kiota.Login.LoginRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>
        /// Instantiates a new <see cref="global::TestHarness.Ext.Http.Kiota.Client.Kiota.KiotaRequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="pathParameters">Path parameters for the request</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public KiotaRequestBuilder(Dictionary<string, object> pathParameters, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/Kiota", pathParameters)
        {
        }
        /// <summary>
        /// Instantiates a new <see cref="global::TestHarness.Ext.Http.Kiota.Client.Kiota.KiotaRequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="rawUrl">The raw URL to use for the request builder.</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public KiotaRequestBuilder(string rawUrl, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/Kiota", rawUrl)
        {
        }
    }
}
