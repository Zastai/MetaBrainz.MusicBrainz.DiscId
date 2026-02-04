// MIT License
//
// Copyright (c) 2016 JetBrains http://www.jetbrains.com
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;

// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming
// ReSharper disable IntroduceOptionalParameters.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace JetBrains.Annotations;

/// <summary>
/// Specifies the details of an implicitly used symbol when it is marked with <see cref="MeansImplicitUseAttribute"/> or
/// <see cref="UsedImplicitlyAttribute"/>.
/// </summary>
[Flags]
internal enum ImplicitUseKindFlags {

  Default = ImplicitUseKindFlags.Access |
            ImplicitUseKindFlags.Assign |
            ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature,

  /// <summary>Only entity marked with attribute considered used.</summary>
  Access = 1,

  /// <summary>Indicates implicit assignment to a member.</summary>
  Assign = 2,

  /// <summary>
  /// Indicates implicit instantiation of a type with a fixed constructor signature.
  /// That means any unused constructor parameters will not be reported as such.
  /// </summary>
  InstantiatedWithFixedConstructorSignature = 4,

  /// <summary>Indicates implicit instantiation of a type.</summary>
  InstantiatedNoFixedConstructorSignature = 8,

}

/// <summary>
/// Specifies what is considered to be used implicitly when marked with <see cref="MeansImplicitUseAttribute"/> or
/// <see cref="UsedImplicitlyAttribute"/>.
/// </summary>
[Flags]
internal enum ImplicitUseTargetFlags {

  Default = ImplicitUseTargetFlags.Itself,

  /// <summary>Code entity itself.</summary>
  Itself = 1,

  /// <summary>Members of the type marked with the attribute are considered used.</summary>
  Members = 2,

  /// <summary> Inherited entities are considered used. </summary>
  WithInheritors = 4,

  /// <summary>Entity marked with the attribute and all its members considered used.</summary>
  WithMembers = ImplicitUseTargetFlags.Itself | ImplicitUseTargetFlags.Members,

}

/// <summary>
/// Can be applied to attributes, type parameters, and parameters of a type assignable from <see cref="System.Type"/>.
/// When applied to an attribute, the decorated attribute behaves the same as <see cref="UsedImplicitlyAttribute"/>.
/// When applied to a type parameter or to a parameter of type <see cref="System.Type"/>,
/// indicates that the corresponding type is used implicitly.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.GenericParameter | AttributeTargets.Parameter)]
internal sealed class MeansImplicitUseAttribute(ImplicitUseKindFlags useKindFlags, ImplicitUseTargetFlags targetFlags) : Attribute {

  public MeansImplicitUseAttribute() : this(ImplicitUseKindFlags.Default, ImplicitUseTargetFlags.Default) {
  }

  public MeansImplicitUseAttribute(ImplicitUseKindFlags useKindFlags) : this(useKindFlags, ImplicitUseTargetFlags.Default) {
  }

  public MeansImplicitUseAttribute(ImplicitUseTargetFlags targetFlags) : this(ImplicitUseKindFlags.Default, targetFlags) {
  }

  public ImplicitUseKindFlags UseKindFlags { get; } = useKindFlags;

  public ImplicitUseTargetFlags TargetFlags { get; } = targetFlags;

}

/// <summary>
/// This attribute is intended to mark publicly available APIs
/// that should not be removed and therefore should never be reported as unused.
/// </summary>
[MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
[AttributeUsage(AttributeTargets.All, Inherited = false)]
internal sealed class PublicAPIAttribute : Attribute {

  public string? Comment { get; init; }

}

/// <summary>
/// Indicates that the marked symbol is used implicitly (via reflection, in an external library, and so on), so this symbol will be
/// ignored by usage-checking inspections.<br/>
/// You can use <see cref="ImplicitUseKindFlags"/> and <see cref="ImplicitUseTargetFlags"/> to configure how this attribute is
/// applied.
/// </summary>
/// <example><code language="cs">
/// [UsedImplicitly]
/// public class TypeConverter { }
///
/// public class SummaryData
/// {
///   [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
///   public SummaryData() { }
/// }
///
/// [UsedImplicitly(ImplicitUseTargetFlags.WithInheritors | ImplicitUseTargetFlags.Default)]
/// public interface IService { }
/// </code></example>
[AttributeUsage(AttributeTargets.All)]
internal sealed class UsedImplicitlyAttribute(ImplicitUseKindFlags useKindFlags, ImplicitUseTargetFlags targetFlags) : Attribute {

  public UsedImplicitlyAttribute() : this(ImplicitUseKindFlags.Default, ImplicitUseTargetFlags.Default) {
  }

  public UsedImplicitlyAttribute(ImplicitUseKindFlags useKindFlags) : this(useKindFlags, ImplicitUseTargetFlags.Default) {
  }

  public UsedImplicitlyAttribute(ImplicitUseTargetFlags targetFlags) : this(ImplicitUseKindFlags.Default, targetFlags) {
  }

  public ImplicitUseKindFlags UseKindFlags { get; } = useKindFlags;

  public ImplicitUseTargetFlags TargetFlags { get; } = targetFlags;

  public string? Reason { get; init; }

}
