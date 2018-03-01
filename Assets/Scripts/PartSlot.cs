public class PartSlot
{
    public PartSchematic Part
    {
        get; private set;
    }

    public PartSchematic.PartType PartType
    {
        get; private set;
    }

    public int Idx
    {
        get; private set;
    }


    public PartSlot(PartSchematic part, PartSchematic.PartType pType, int idx) {
        Part = part;
        PartType = pType;
        Idx = idx;
    }

    public void UpdatePart(PartSchematic part) {
        Part = part;
    }
}