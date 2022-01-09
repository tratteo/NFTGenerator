// Copyright Matteo Beltrame

namespace NFTGenerator.Objects;

internal class LayerPick
{
    public Layer Layer { get; init; }

    public Asset Asset { get; init; }

    public bool Equals(LayerPick? other) => Layer.Index.Equals(other.Layer.Index) && Asset.Id.Equals(other.Asset.Id);
}