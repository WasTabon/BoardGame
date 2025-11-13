public enum AnimalType
{
    None,
    Tiger,
    Rabbit,
    Dragon
}

public enum Player
{
    None,
    PlayerA,
    PlayerB
}

public static class AnimalRules
{
    // Tiger > Rabbit > Dragon > Tiger
    public static bool Dominates(AnimalType attacker, AnimalType defender)
    {
        if (attacker == AnimalType.Tiger && defender == AnimalType.Rabbit) return true;
        if (attacker == AnimalType.Rabbit && defender == AnimalType.Dragon) return true;
        if (attacker == AnimalType.Dragon && defender == AnimalType.Tiger) return true;
        return false;
    }
}