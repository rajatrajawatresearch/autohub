using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

namespace AutoHub.Persistence.Naming;

/// <summary>
/// Removes the reflection scratch state EF's nullability conventions leave on the finalized model.
/// </summary>
/// <remarks>
/// EF's non-nullable-reference-type conventions cache a <see cref="NullabilityInfoContext"/> in a
/// model annotation while they run. On this model it survives finalization, and the migration
/// scaffolder — which serialises every annotation it does not recognise as
/// <c>.HasAnnotation(name, value)</c> — then tries to emit a C# literal for a reflection helper and
/// fails with <c>Cannot scaffold C# literals of type 'System.Reflection.NullabilityInfoContext'</c>.
/// <para>
/// The annotation is a cache, not configuration: nothing reads it after the conventions that wrote
/// it have finished, and the shape of the model does not depend on it. Dropping it in a convention
/// that runs after the built-in ones is therefore safe, and it is the only intervention that
/// happens late enough — removing it inside <c>OnModelCreating</c> is too early, because the
/// conventions run afterwards and put it back.
/// </para>
/// <para>
/// Matched by value type rather than by annotation name deliberately. The name is an EF
/// implementation detail that has changed between releases; the type is what the scaffolder actually
/// chokes on, so this keeps working across an upgrade instead of silently stopping.
/// </para>
/// </remarks>
internal sealed class DropConventionScratchStateConvention : IModelFinalizingConvention
{
    public void ProcessModelFinalizing(
        IConventionModelBuilder modelBuilder,
        IConventionContext<IConventionModelBuilder> context)
    {
        var model = modelBuilder.Metadata;

        foreach (var annotation in model.GetAnnotations().ToList())
        {
            if (annotation.Value is NullabilityInfoContext)
            {
                model.RemoveAnnotation(annotation.Name);
            }
        }
    }
}
