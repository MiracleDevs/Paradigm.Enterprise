using Paradigm.Enterprise.WebApi.Middlewares;

namespace Paradigm.Web.ApiTemplate.WebApi.Middlewares
{
    public class AuthenticationMiddleware : MiddlewareBase
    {
        #region Properties

        /// <summary>
        /// The authentication header
        /// </summary>
        public static readonly string AuthenticationHeader = "authorization";

        #endregion

        #region Constructor

        public AuthenticationMiddleware(RequestDelegate next) : base(next)
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Invokes the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        public override async Task Invoke(HttpContext context)
        {
            if (context.Request.Headers.ContainsKey(AuthenticationHeader))
            {
                //var provider = context.RequestServices.GetRequiredService<Paradigm.Web.ApiTemplate.Providers.Security.IUserProvider > ();
                //if (provider is not null)
                //{
                //    var authToken = context.Request.Headers[AuthenticationHeader];
                //    if (Guid.TryParse(authToken.ToString(), out var guidToken))
                //    {
                //        await provider.AuthenticateRequestAsync(guidToken);
                //    }
                //}
            }

            await Next.Invoke(context);
        }

        #endregion
    }
}
