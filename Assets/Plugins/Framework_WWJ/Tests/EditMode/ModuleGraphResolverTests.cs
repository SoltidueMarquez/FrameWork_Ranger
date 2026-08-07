using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Framework_WWJ.Tests
{
    internal sealed class ModuleGraphResolverTests
    {
        private readonly List<Object> m_createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < m_createdObjects.Count; i++)
            {
                Object.DestroyImmediate(m_createdObjects[i]);
            }

            m_createdObjects.Clear();
        }

        [Test]
        public void Resolve_DependencyWinsOverPriority_ThenUsesStableReadyOrder()
        {
            var moduleA = CreateModule<TestModuleA>(100);
            var moduleB = CreateModule<TestModuleB>(-100);
            var moduleC = CreateModule<TestModuleC>(0);
            var global = CreateGlobal(moduleA, moduleB, moduleC);
            var scene = CreateScene();

            var result = ModuleGraphResolver.Resolve(global, scene);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.OrderedGlobalNodes[0].ModuleType, Is.EqualTo(typeof(TestModuleC)));
            Assert.That(result.OrderedGlobalNodes[1].ModuleType, Is.EqualTo(typeof(TestModuleA)));
            Assert.That(result.OrderedGlobalNodes[2].ModuleType, Is.EqualTo(typeof(TestModuleB)));
        }

        [Test]
        public void Resolve_SamePriority_PreservesConfigOrder()
        {
            var moduleA = CreateModule<TestModuleA>(0);
            var moduleC = CreateModule<TestModuleC>(0);
            var result = ModuleGraphResolver.Resolve(
                CreateGlobal(moduleC, moduleA),
                CreateScene());

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.OrderedGlobalNodes[0].ModuleType, Is.EqualTo(typeof(TestModuleC)));
            Assert.That(result.OrderedGlobalNodes[1].ModuleType, Is.EqualTo(typeof(TestModuleA)));
        }

        [Test]
        public void Resolve_SceneMayDependOnGlobal()
        {
            var result = ModuleGraphResolver.Resolve(
                CreateGlobal(CreateModule<TestModuleA>()),
                CreateScene(CreateModule<TestSceneDependsGlobalModule>()));

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.OrderedSceneNodes.Count, Is.EqualTo(1));
        }

        [Test]
        public void Resolve_RejectsDuplicateMissingAndCycle()
        {
            var duplicateA = CreateModule<TestModuleA>();
            var duplicateB = CreateModule<TestModuleA>();
            var missing = CreateModule<TestMissingDependencyModule>();
            var cycleA = CreateModule<TestCycleModuleA>();
            var cycleB = CreateModule<TestCycleModuleB>();

            var result = ModuleGraphResolver.Resolve(
                CreateGlobal(duplicateA, duplicateB, missing, cycleA, cycleB),
                CreateScene());

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Diagnostics.Exists(ModuleGraphDiagnosticCode.DuplicateType), Is.True);
            Assert.That(result.Diagnostics.Exists(ModuleGraphDiagnosticCode.MissingDependency), Is.True);
            Assert.That(result.Diagnostics.Exists(ModuleGraphDiagnosticCode.DependencyCycle), Is.True);
        }

        [Test]
        public void Resolve_RejectsGlobalToSceneDirection_AndSelfDependency()
        {
            var result = ModuleGraphResolver.Resolve(
                CreateGlobal(
                    CreateModule<TestGlobalDependsSceneModule>(),
                    CreateModule<TestSelfDependencyModule>()),
                CreateScene(CreateModule<TestSceneOnlyModule>()));

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Diagnostics.Exists(ModuleGraphDiagnosticCode.InvalidScopeDirection), Is.True);
            Assert.That(result.Diagnostics.Exists(ModuleGraphDiagnosticCode.DependencyCycle), Is.True);
        }

        [Test]
        public void Resolve_DisabledDependencyIsMissing_AndEmptyHandlerIsInvalid()
        {
            var dependency = CreateModule<TestModuleA>();
            var dependent = CreateModule<TestModuleB>();
            var emptyHandler = CreateModule<TestHandlerModule>();
            var global = ScriptableObject.CreateInstance<FrameworkGlobalConfig>();
            global.SetModules(new List<ModuleConfigEntry>
            {
                new ModuleConfigEntry(false, dependency),
                new ModuleConfigEntry(true, dependent),
                new ModuleConfigEntry(true, emptyHandler),
            });
            m_createdObjects.Add(global);

            var result = ModuleGraphResolver.Resolve(global, CreateScene());

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Diagnostics.Exists(ModuleGraphDiagnosticCode.MissingDependency), Is.True);
            Assert.That(result.Diagnostics.Exists(ModuleGraphDiagnosticCode.MissingHandler), Is.True);
        }

        private T CreateModule<T>(int priority = 0) where T : ModuleBase
        {
            var module = ScriptableObject.CreateInstance<T>();
            module.SetLoadPriority(priority);
            m_createdObjects.Add(module);
            return module;
        }

        private FrameworkGlobalConfig CreateGlobal(params ModuleBase[] modules)
        {
            var config = ScriptableObject.CreateInstance<FrameworkGlobalConfig>();
            var entries = new List<ModuleConfigEntry>();
            for (var i = 0; i < modules.Length; i++)
            {
                entries.Add(new ModuleConfigEntry(true, modules[i]));
            }

            config.SetModules(entries);
            m_createdObjects.Add(config);
            return config;
        }

        private FrameworkSceneConfig CreateScene(params ModuleBase[] modules)
        {
            var config = ScriptableObject.CreateInstance<FrameworkSceneConfig>();
            var entries = new List<ModuleConfigEntry>();
            for (var i = 0; i < modules.Length; i++)
            {
                entries.Add(new ModuleConfigEntry(true, modules[i]));
            }

            config.SetModules(entries);
            m_createdObjects.Add(config);
            return config;
        }
    }

    internal static class DiagnosticTestExtensions
    {
        internal static bool Exists(
            this IReadOnlyList<ModuleGraphDiagnostic> diagnostics,
            ModuleGraphDiagnosticCode code)
        {
            for (var i = 0; i < diagnostics.Count; i++)
            {
                if (diagnostics[i].Code == code)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
