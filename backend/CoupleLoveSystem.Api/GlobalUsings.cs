// 全局 using：补齐各层之间（生成时）系统性遗漏的命名空间引用，消除大量 CS0246/CS1061/CS0234
global using CoupleLoveSystem.Core.Entities;
global using CoupleLoveSystem.Core.Enums;
global using CoupleLoveSystem.Core.Result;
global using CoupleLoveSystem.Infrastructure.Persistence;
global using CoupleLoveSystem.Application.Services;
global using Microsoft.EntityFrameworkCore;
global using Serilog;
global using System.Collections.Concurrent;
