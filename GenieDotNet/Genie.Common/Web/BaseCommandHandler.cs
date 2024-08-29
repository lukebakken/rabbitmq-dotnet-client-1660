using Google.Protobuf;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IO;
using Microsoft.Net.Http.Headers;
using System.Linq.Expressions;
using System.Reflection;

namespace Genie.Common.Web;

public class BaseCommandHandler(GenieContext genieContext)
{
    private static readonly RecyclableMemoryStreamManager manager = new RecyclableMemoryStreamManager();
    protected GenieContext Context => genieContext;
}