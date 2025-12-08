// Timberborn Mod: SmartPower
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using Bindito.Core;
using IgorZ.TimberDev.Utils;
using Timberborn.PowerGenerating;
using Timberborn.TemplateInstantiation;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local

namespace IgorZ.SmartPower.PowerGenerators;

[Context("Game")]
// ReSharper disable once UnusedType.Global
sealed class Configurator : IConfigurator {
  static readonly string PatchId = typeof(Configurator).AssemblyQualifiedName;

  public void Configure(IContainerDefinition containerDefinition) {
    HarmonyPatcher.PatchRepeated(PatchId, typeof(GoodPoweredGeneratorPatch));
    containerDefinition.Bind<SmartGoodConsumingGenerator>().AsSingleton();
    containerDefinition.Bind<SmartWalkerPoweredGenerator>().AsSingleton();
    containerDefinition.MultiBind<TemplateModule>().ToProvider(ProvideTemplateModule).AsSingleton();
  }

  static TemplateModule ProvideTemplateModule() {
    var builder = new TemplateModule.Builder();
    builder.AddDecorator<GoodPoweredGeneratorSpec, SmartGoodConsumingGenerator>();
    builder.AddDecorator<WalkerPoweredGeneratorSpec, SmartWalkerPoweredGenerator>();
    return builder.Build();
  }
}
