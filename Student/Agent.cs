using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

class Agent : BaseAgent
{
    [STAThread]
    static void Main()
    {
        Program.Start(new Agent());
    }

    public Agent() { }

    // Hjälpmetod för BFS
    private List<Point> HittaKortasteVäg(SpelBräde bräde, Point start, int målraden)
    {
        var kö = new Queue<Point>();
        var besökta = new HashSet<Point>();
        var föregångare = new Dictionary<Point, Point>();

        kö.Enqueue(start);
        besökta.Add(start);

        while (kö.Count > 0)
        {
            var aktuell = kö.Dequeue();

            // Om vi har nått målraden, bygg vägen tillbaka
            if (aktuell.Y == målraden)
            {
                var väg = new List<Point>();
                while (föregångare.ContainsKey(aktuell))
                {
                    väg.Add(aktuell);
                    aktuell = föregångare[aktuell];
                }
                väg.Reverse();
                return väg;
            }

            // Kolla alla möjliga rörelser
            foreach (var granne in HämtaGrannar(bräde, aktuell))
            {
                if (!besökta.Contains(granne))
                {
                    kö.Enqueue(granne);
                    besökta.Add(granne);
                    föregångare[granne] = aktuell;
                }
            }
        }

        return null; // Ingen väg hittades
    }

    // Hämta grannar (möjliga drag från en given ruta)
    private List<Point> HämtaGrannar(SpelBräde bräde, Point position)
    {
        var grannar = new List<Point>();
        var x = position.X;
        var y = position.Y;

        // Uppåt
        if (y < SpelBräde.N - 1 && !bräde.horisontellaVäggar[x, y])
        {
            grannar.Add(new Point(x, y + 1));
        }
        // Nedåt
        if (y > 0 && !bräde.horisontellaVäggar[x, y - 1])
        {
            grannar.Add(new Point(x, y - 1));
        }
        // Höger
        if (x < SpelBräde.N - 1 && !bräde.vertikalaVäggar[x, y])
        {
            grannar.Add(new Point(x + 1, y));
        }
        // Vänster
        if (x > 0 && !bräde.vertikalaVäggar[x - 1, y])
        {
            grannar.Add(new Point(x - 1, y));
        }

        return grannar;
    }

    public override Drag SökNästaDrag(SpelBräde bräde)
    {
        Spelare jag = bräde.spelare[0];
        Spelare motståndare = bräde.spelare[1]; // Antag att motståndaren är på index 1

        int målradenAgent = 8;    // Agentens mål är högst upp
        int målradenMotståndare = 0; // Motståndarens mål är längst ner

        // Hitta kortaste väg för agenten
        var minVäg = HittaKortasteVäg(bräde, jag.position, målradenAgent);

        // Kontrollera om agenten har kommit halvvägs
        if (motståndare.position.Y <= 4) // Halvvägs på ett 8x8-bräde
        {
            // Hitta kortaste väg för motståndaren
            var motståndarensVäg = HittaKortasteVäg(bräde, motståndare.position, målradenMotståndare);

            // Om motståndaren har en väg och vi kan blockera den, sätt en vägg
            if (motståndarensVäg != null && motståndarensVäg.Count > 1 && jag.antalVäggar > 0)
            {
                var blockeraPunkt = motståndarensVäg[1]; // Nästa steg i motståndarens väg

                // Försök placera en horisontell vägg nära motståndarens väg, men kontrollera om det skulle blockera motståndaren helt
                if (KanPlaceraHorisontellVägg(bräde, blockeraPunkt) && KanMotståndarenFortsätta(bräde, motståndare))
                {
                    return new Drag
                    {
                        typ = Typ.Horisontell,
                        point = blockeraPunkt
                    };
                }

                // Försök placera en vertikal vägg nära motståndarens väg, men kontrollera om det skulle blockera motståndaren helt
                if (KanPlaceraVertikalVägg(bräde, blockeraPunkt) && KanMotståndarenFortsätta(bräde, motståndare))
                {
                    return new Drag
                    {
                        typ = Typ.Vertikal,
                        point = blockeraPunkt
                    };
                }
            }
        }

        // Om ingen vägg ska sättas eller om vi inte har några väggar kvar, flytta
        if (minVäg != null && minVäg.Count > 0)
        {
            var nästaPosition = minVäg[0];
            return new Drag
            {
                typ = Typ.Flytta,
                point = nästaPosition
            };
        }

        // Om ingen väg hittas, stå still som fallback
        return new Drag
        {
            typ = Typ.Flytta,
            point = jag.position
        };
    }

    // Kontrollera om motståndaren fortfarande kan hitta en väg till sitt mål
    private bool KanMotståndarenFortsätta(SpelBräde bräde, Spelare motståndare)
    {
        int målradenMotståndare = 0; // Motståndarens mål är längst ner
        var motståndarensVäg = HittaKortasteVäg(bräde, motståndare.position, målradenMotståndare);

        // Om motståndaren inte har någon väg till målet, returnera false
        return motståndarensVäg != null && motståndarensVäg.Count > 0;
    }


    private bool KanPlaceraHorisontellVägg(SpelBräde bräde, Point position)
    {
        int x = position.X;
        int y = position.Y;

        // Kontrollera om x och y är inom de tillåtna gränserna för väggens längd
        if (x < 0 || x >= SpelBräde.N - 1 || y < 0 || y >= SpelBräde.N - 1)
            return false;

        // Kontrollera om det redan finns en vägg här, inklusive nästa ruta för en 2-rutors vägg
        if (bräde.horisontellaVäggar[x, y] || bräde.horisontellaLångaVäggar[x, y] ||
            bräde.horisontellaVäggar[x + 1, y] || bräde.horisontellaLångaVäggar[x + 1, y])
            return false;

        return true;
    }

    private bool KanPlaceraVertikalVägg(SpelBräde bräde, Point position)
    {
        int x = position.X;
        int y = position.Y;

        // Kontrollera om x och y är inom de tillåtna gränserna för väggens längd
        if (x < 0 || x >= SpelBräde.N - 1 || y < 0 || y >= SpelBräde.N - 1)
            return false;

        // Kontrollera om det redan finns en vägg här, inklusive nästa ruta för en 2-rutors vägg
        if (bräde.vertikalaVäggar[x, y] || bräde.vertikalaLångaVäggar[x, y] ||
            bräde.vertikalaVäggar[x, y + 1] || bräde.vertikalaLångaVäggar[x, y + 1])
            return false;

        return true;
    }





    public override Drag GörOmDrag(SpelBräde bräde, Drag drag)
    {
        // Om draget var ogiltigt, välj ett nytt
        return SökNästaDrag(bräde);
    }
}