using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text;

namespace Paradigm.Web.ApiTemplate.HealthChecks;

public static class HealthCheckCustomResponse
{
    public static string GetResult(string applicationTitle, HealthReport report)
    {
        var body = new StringBuilder();

        body.Append(@$"
<html>
    <head>
        <title>{applicationTitle}</title>
        <style>

            * {{
                box-sizing: border-box;
                padding: 0;
                margin: 0;
            }}
            body {{
                font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;
                display: flex;
                flex-direction: column;
                gap: 2rem;
                padding: 0;
                margin: 0;
                color: #303030;
                background-color: #faf9f8;
            }}

            header {{
                display: flex;
                flex-direction: row;
                gap: 50px;
                padding: 20px;
            }}

            header > div {{
                display: flex;
                flex-direction: column;
                justify-content: center;
            }}

            footer {{
                bottom: 0;
                width: 100%;
                color: #fff;
                background: #303030;
                display: flex;
                place-content: center;
                place-items: center;
                padding: 5px;
                gap: 20px;
            }}

            main {{
                display: grid;
                grid-template: 1fr;
                padding: 20px;
                gap: clamp(10px, 10vw, 50px) clamp(10px, 10vw, 30px);
            }}

            h1 {{
                font-weight: 700;
                font-size: 24px;
            }}

            p {{
                font-size: 15px;
            }}

            section {{
                display: flex;
                flex-direction: column;
                gap: 1rem;
                border-radius: 4px;
                width: 100%;
                box-shadow: rgb(0 0 0 / 13%) 0px 2px 8px;
                transition: box-shadow 0.6s ease 0s;
                padding: 25px;
                background-color: white;
            }}

            section:hover {{
                box-shadow: 0 3.6px 5.6px 0 rgba(0, 0, 0, 0.132), 0 2.3px 2.9px 0 rgba(0, 0, 0, 0.108);
            }}

            @media (min-width: 768px) {{
                main {{
                    grid-template: none / repeat(1, 1fr);
                }}
            }}

            @media (min-width: 992px) {{
                main {{
                    grid-template: none / repeat(1, 1fr);
                }}
            }}

        </style>
    </head>
    <body>
        <header>
            <div>
                <h1>{applicationTitle}</h1>
            </div>
        </header>
        <main>");

        foreach (var entry in report.Entries)
        {
            body.Append(@$"
            <section>
                <span><b>Title</b>: {entry.Key}</span>
                <span><b>Description</b>: {entry.Value.Description}</span>
                <span><b>Duration</b>: {entry.Value.Duration} </span>
                <span><b>Status</b>: {entry.Value.Status} </span>
                    {(entry.Value.Exception != null
                    ? $"<span><b>Exception</b>: {entry.Value.Exception} </span>"
                    : "")}
            </section>");
        }

        body.Append($@"
        </main>
        <footer>
            <span><b>Run Date:</b> {DateTime.Now.ToString()}</span>
            <span><b>Status:</b> {report.Status}</span>
        </footer>
    </body>
 </html>");

        return body.ToString();
    }
}