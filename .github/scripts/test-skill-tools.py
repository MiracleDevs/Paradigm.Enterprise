from __future__ import annotations

import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path

sys.dont_write_bytecode = True

ROOT = Path(__file__).resolve().parents[2]


def load_module(name: str, relative_path: str):
    path = ROOT / relative_path
    spec = importlib.util.spec_from_file_location(name, path)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Could not load {path}")
    module = importlib.util.module_from_spec(spec)
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


SCAFFOLDER = load_module(
    "paradigm_scaffolder",
    ".agents/skills/paradigm-setup-project/scripts/scaffold_from_template.py",
)
DATABASE_VALIDATOR = load_module(
    "paradigm_database_validator",
    ".agents/skills/paradigm-build-database/scripts/validate_database_project.py",
)


class ScaffoldFromTemplateTests(unittest.TestCase):
    def create_template(self, root: Path) -> tuple[Path, bytes, str]:
        source = root / "template" / "src"
        project = source / "Paradigm.Web.ApiTemplate.WebApi"
        project.mkdir(parents=True)
        guid = "{11111111-2222-3333-4444-555555555555}"
        (source / "Paradigm.Web.ApiTemplate.sln").write_text(
            f'Project("{guid}") = "Paradigm.Web.ApiTemplate.WebApi", '
            '"Paradigm.Web.ApiTemplate.WebApi\\Paradigm.Web.ApiTemplate.WebApi.csproj", '
            f'"{guid}"\n',
            encoding="utf-8",
        )
        (project / "Paradigm.Web.ApiTemplate.WebApi.csproj").write_text(
            '<Project Sdk="Microsoft.NET.Sdk">\n'
            '  <ItemGroup><PackageReference Include="Paradigm.Enterprise.WebApi" Version="1.0.23" /></ItemGroup>\n'
            '</Project>\n',
            encoding="utf-8",
        )
        (project / "Program.cs").write_text(
            "namespace $ext_safeprojectname$.WebApi;\n",
            encoding="utf-8",
        )
        binary = b"\x00\x01\xfftemplate-binary"
        (project / "icon.ico").write_bytes(binary)
        return source.parent, binary, guid

    def test_dry_run_does_not_write(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            template, _, _ = self.create_template(root)
            output = root / "output"
            targets = SCAFFOLDER.scaffold(template, "Contoso.Product", output, "1.1.0", True)
            self.assertGreater(len(targets), 0)
            self.assertFalse(output.exists())

    def test_scaffold_replaces_tokens_guids_and_preserves_binary(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            template, binary, old_guid = self.create_template(root)
            source_snapshot = {
                path.relative_to(template): path.read_bytes()
                for path in template.rglob("*")
                if path.is_file()
            }
            output = root / "output"
            SCAFFOLDER.scaffold(template, "Contoso.Product", output, "1.1.0", False)

            project = output / "src" / "Contoso.Product.WebApi" / "Contoso.Product.WebApi.csproj"
            self.assertTrue(project.is_file())
            self.assertIn('Version="1.1.0"', project.read_text(encoding="utf-8"))
            self.assertIn(
                "namespace Contoso.Product.WebApi;",
                (project.parent / "Program.cs").read_text(encoding="utf-8"),
            )
            solution = (output / "src" / "Contoso.Product.sln").read_text(encoding="utf-8")
            self.assertNotIn(old_guid, solution)
            self.assertEqual(binary, (project.parent / "icon.ico").read_bytes())
            self.assertEqual(
                source_snapshot,
                {
                    path.relative_to(template): path.read_bytes()
                    for path in template.rglob("*")
                    if path.is_file()
                },
            )

    def test_scaffold_refuses_nonempty_output(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            template, _, _ = self.create_template(root)
            output = root / "output"
            output.mkdir()
            (output / "owned.txt").write_text("user data", encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "must be empty"):
                SCAFFOLDER.scaffold(template, "Contoso.Product", output, "1.1.0", False)

    def test_scaffold_preserves_slnx_format(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            template, _, _ = self.create_template(root)
            solution = template / "src" / "Paradigm.Web.ApiTemplate.sln"
            solution.rename(solution.with_suffix(".slnx"))
            output = root / "output"
            SCAFFOLDER.scaffold(template, "Contoso.Product", output, "1.1.0", False)
            self.assertTrue((output / "src" / "Contoso.Product.slnx").is_file())
            self.assertFalse((output / "src" / "Contoso.Product.sln").exists())


class DatabaseValidatorTests(unittest.TestCase):
    required_folders = (
        "tables/Security",
        "views/Security",
        "functions",
        "routines",
        "types",
        "scripts/predeployment",
        "scripts/postdeployment/Security",
        "scripts/maintenance",
    )

    def create_folders(self, root: Path) -> None:
        for folder in self.required_folders:
            (root / folder).mkdir(parents=True, exist_ok=True)

    def create_sql_project(self, root: Path, unique_prefix: str = "UQ") -> tuple[Path, Path]:
        self.create_folders(root)
        project = root / "Product.Database.sqlproj"
        project.write_text(
            '<Project DefaultTargets="Build">\n'
            '  <Sdk Name="Microsoft.Build.Sql" Version="2.1.0" />\n'
            '  <ItemGroup>\n'
            '    <PreDeploy Include="scripts\\predeployment\\PreDeployment.sql" />\n'
            '    <PostDeploy Include="scripts\\postdeployment\\PostDeployment.sql" />\n'
            '    <Build Remove="scripts\\postdeployment\\**\\*.sql" />\n'
            '    <Build Remove="scripts\\maintenance\\**\\*.sql" />\n'
            '  </ItemGroup>\n'
            '</Project>\n',
            encoding="utf-8",
        )
        (root / "tables/Security/User.sql").write_text(
            "CREATE TABLE [dbo].[User] (\n"
            " [Id] INT IDENTITY(1,1) NOT NULL,\n"
            " [Email] NVARCHAR(200) NOT NULL,\n"
            " [CreatedByUserId] INT NULL, [CreationDate] DATETIMEOFFSET(7) NOT NULL,\n"
            " [ModifiedByUserId] INT NULL, [ModificationDate] DATETIMEOFFSET(7) NULL,\n"
            " [IsActive] BIT NOT NULL,\n"
            " CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED ([Id]),\n"
            f" CONSTRAINT [{unique_prefix}_User_Email] UNIQUE ([Email])\n"
            ");\n",
            encoding="utf-8",
        )
        (root / "views/Security/UserView.sql").write_text(
            "CREATE VIEW [dbo].[UserView] WITH SCHEMABINDING AS SELECT [Id], [Email] FROM [dbo].[User];\n",
            encoding="utf-8",
        )
        (root / "scripts/predeployment/PreDeployment.sql").write_text("-- reserved\n", encoding="utf-8")
        (root / "scripts/postdeployment/PostDeployment.sql").write_text(
            ":r .\\Security\\RoleData.sql\n", encoding="utf-8"
        )
        (root / "scripts/postdeployment/Security/RoleData.sql").write_text(
            "MERGE INTO [dbo].[Role] AS target USING (SELECT 1 AS Id) AS source ON target.Id = source.Id "
            "WHEN NOT MATCHED THEN INSERT (Id) VALUES (source.Id);\n",
            encoding="utf-8",
        )
        (root / "bootstrap").mkdir()
        (root / "bootstrap/Product.bacpac").write_bytes(b"baseline")
        solution = root.parent / "Product.slnx"
        solution.write_text(
            '<Solution><Project Path="database/Product.Database.sqlproj" /></Solution>', encoding="utf-8"
        )
        return project, solution

    def create_postgresql_project(self, root: Path) -> Path:
        self.create_folders(root)
        (root / "tables/Security/User.sql").write_text(
            'CREATE TABLE IF NOT EXISTS "User" ("Id" INT NOT NULL, '
            '"IsActive" BOOLEAN NOT NULL DEFAULT true, '
            'CONSTRAINT "PK_User" PRIMARY KEY ("Id"));',
            encoding="utf-8",
        )
        (root / "views/Security/UserView.sql").write_text(
            'CREATE OR REPLACE VIEW "UserView" AS SELECT "Id" FROM "User";', encoding="utf-8"
        )
        (root / "scripts/predeployment/Database.sql").write_text("SELECT 1;", encoding="utf-8")
        (root / "routines/SearchUsers.sql").write_text("SELECT 1;", encoding="utf-8")
        (root / "scripts/postdeployment/Security/RoleData.sql").write_text(
            'INSERT INTO "Role" ("Id") VALUES (1) ON CONFLICT ("Id") DO UPDATE SET "Id" = EXCLUDED."Id";',
            encoding="utf-8",
        )
        project = root / "project.jsonc"
        project.write_text(
            json.dumps(
                {
                    "databaseType": "PostgreSql",
                    "generateScript": True,
                    "executeScript": True,
                    "outputFileName": "publish.sql",
                    "files": [
                        "scripts/predeployment/Database.sql",
                        "tables/Security/User.sql",
                        "views/Security/UserView.sql",
                        "routines/SearchUsers.sql",
                        "scripts/postdeployment/Security/RoleData.sql",
                    ],
                }
            ),
            encoding="utf-8",
        )
        return project

    def codes(self, diagnostics) -> list[str]:
        return [diagnostic.code for diagnostic in diagnostics]

    def test_valid_sql_server_project_has_no_diagnostics(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory) / "database"
            root.mkdir()
            project, solution = self.create_sql_project(root)
            diagnostics = DATABASE_VALIDATOR.Validator(project, solution, True).validate()
            self.assertEqual([], diagnostics)

    def test_legacy_ux_is_warning_or_strict_error(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory) / "database"
            root.mkdir()
            project, solution = self.create_sql_project(root, unique_prefix="UX")
            relaxed = DATABASE_VALIDATOR.Validator(project, solution, False).validate()
            strict = DATABASE_VALIDATOR.Validator(project, solution, True).validate()
            self.assertIn("warning", [item.severity for item in relaxed if item.code == "PEDB101"])
            self.assertIn("error", [item.severity for item in strict if item.code == "PEDB101"])

    def test_sql_server_detects_json_password(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory) / "database"
            root.mkdir()
            project, solution = self.create_sql_project(root)
            (root / "settings.json").write_text('{"password": "secret"}', encoding="utf-8")
            diagnostics = DATABASE_VALIDATOR.Validator(project, solution, True).validate()
            self.assertIn("PEDB003", self.codes(diagnostics))

    def test_sql_server_detects_missing_and_duplicate_includes(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory) / "database"
            root.mkdir()
            project, solution = self.create_sql_project(root)
            text = project.read_text(encoding="utf-8")
            text = text.replace(
                "  </ItemGroup>",
                '    <None Include="missing.sql" />\n'
                '    <None Include="scripts\\predeployment\\PreDeployment.sql" />\n'
                "  </ItemGroup>",
            )
            project.write_text(text, encoding="utf-8")
            diagnostics = DATABASE_VALIDATOR.Validator(project, solution, True).validate()
            messages = [item.message for item in diagnostics if item.code == "PEDB105"]
            self.assertTrue(any("missing file" in message for message in messages))
            self.assertTrue(any("Duplicate" in message for message in messages))

    def test_valid_postgresql_project_has_no_diagnostics(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory) / "database"
            root.mkdir()
            project = self.create_postgresql_project(root)
            diagnostics = DATABASE_VALIDATOR.Validator(project, None, True).validate()
            self.assertEqual([], diagnostics)

    def test_postgresql_detects_secret_duplicate_missing_and_order(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory) / "database"
            root.mkdir()
            project = self.create_postgresql_project(root)
            configuration = json.loads(project.read_text(encoding="utf-8"))
            configuration["connectionString"] = "Host=localhost;Password=secret"
            configuration["executeScript"] = "yes"
            configuration["files"] = [
                "views/Security/UserView.sql",
                "tables/Security/User.sql",
                "tables/Security/User.sql",
                "tables/Missing.sql",
            ]
            configuration["paths"] = ["routines", "missing-path"]
            project.write_text(json.dumps(configuration), encoding="utf-8")
            diagnostics = DATABASE_VALIDATOR.Validator(project, None, True).validate()
            codes = self.codes(diagnostics)
            self.assertIn("PEDB003", codes)
            self.assertIn("PEDB201", codes)
            self.assertIn("PEDB202", codes)
            self.assertIn("PEDB203", codes)
            self.assertEqual(diagnostics, DATABASE_VALIDATOR.Validator(project, None, True).validate())


if __name__ == "__main__":
    unittest.main()
