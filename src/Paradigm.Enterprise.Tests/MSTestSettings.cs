global using Microsoft.VisualStudio.TestTools.UnitTesting;
global using Moq;
global using Paradigm.Enterprise.Domain.Entities;
global using Paradigm.Enterprise.Domain.Repositories;
global using Paradigm.Enterprise.Domain.Uow;
global using Paradigm.Enterprise.Interfaces;
global using Paradigm.Enterprise.Providers;
global using System.Diagnostics.CodeAnalysis;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]
