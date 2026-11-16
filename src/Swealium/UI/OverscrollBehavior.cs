using Swealium.Graphics;
namespace Swealium.UI;

public enum OverscrollBehavior { Auto, Chain, Contain, None }
/// <summary>Normalized wheel delta remaining after this container reaches a boundary.</summary>
public sealed record ScrollBoundaryEvent(Point UnconsumedDelta,Point Offset);
