// Copyright Matteo Beltrame

using System;

namespace NFTGenerator.Objects;

internal class LayerPick : IEquatable<LayerPick>
{
    public Layer Layer { get; init; }

    public Asset Asset { get; init; }

    public bool Equals(LayerPick? other) => other is null || Layer.Index.Equals(other.Layer.Index) && Asset.Id.Equals(other.Asset.Id);
}