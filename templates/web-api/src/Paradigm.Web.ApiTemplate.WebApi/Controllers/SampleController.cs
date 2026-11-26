// using Microsoft.AspNetCore.Mvc;
// using Paradigm.Enterprise.WebApi.Controllers;
// using Paradigm.Web.ApiTemplate.Domain.Sample.Entities;
// using Paradigm.Web.ApiTemplate.Providers.Sample;

// namespace Paradigm.Web.ApiTemplate.WebApi.Controllers;

// [Route("[controller]")]
// public class SampleController : ApiControllerBase<ISampleProvider>
// {
//     public SampleController(ILogger<ApiControllerBase> logger, ISampleProvider provider) : base(logger, provider)
//     {
//     }

//     [HttpGet("get-all")]
//     public async Task<IEnumerable<SampleView>> GetAllAsync()
//     {
//         return await Provider.GetAllAsync();
//     }
// }
