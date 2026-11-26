using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Context;

namespace Paradigm.Web.ApiTemplate.Data.Sample.Context;

// TODO: Remove this file and generate providers and interfaces in this folder.
// Adding this file to make compiler to generate the namespace and avoid compilation errors.

public class SampleDbContext : DbContextBase<int>
{
    public SampleDbContext(IServiceProvider serviceProvider, DbContextOptions<SampleDbContext> options)
        : base(serviceProvider, options)
    {
    }
}

