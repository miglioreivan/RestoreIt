using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Test unitari per le logiche di calcolo puro dei gestori di restauro.
/// Eseguibili dal Test Runner di Unity (Window > General > Test Runner > EditMode).
/// </summary>
public class RestorationProgressionTests
{
    // ─────────────────────────────────────────────────────────────
    // Progressione Colla / Pulizia
    // ─────────────────────────────────────────────────────────────

    [Test]
    public void ProgressioneColla_ZeroPixelDipinti_ReturnsZero()
    {
        int dipinti    = 0;
        int totale     = 100;
        float progresso = RestorationUtils.CalcolaProgressione(dipinti, totale);

        Assert.AreEqual(0f, progresso);
    }

    [Test]
    public void ProgressioneColla_TuttiPixelDipinti_ReturnsOne()
    {
        int dipinti    = 100;
        int totale     = 100;
        float progresso = RestorationUtils.CalcolaProgressione(dipinti, totale);

        Assert.AreEqual(1f, progresso);
    }

    [Test]
    public void ProgressioneColla_MetaPixelDipinti_ReturnsHalf()
    {
        int dipinti    = 50;
        int totale     = 100;
        float progresso = RestorationUtils.CalcolaProgressione(dipinti, totale);

        Assert.AreEqual(0.5f, progresso, 0.001f);
    }

    [Test]
    public void ProgressioneColla_TotaleZero_ReturnsZero()
    {
        int dipinti    = 0;
        int totale     = 0;
        float progresso = RestorationUtils.CalcolaProgressione(dipinti, totale);

        Assert.AreEqual(0f, progresso, "Con totale pari a zero la progressione deve essere 0 per evitare divisioni per zero.");
    }

    [Test]
    public void ProgressioneColla_NonSuperaUno()
    {
        int dipinti    = 150; // più del totale (caso anomalo)
        int totale     = 100;
        float progresso = RestorationUtils.CalcolaProgressione(dipinti, totale);

        Assert.LessOrEqual(progresso, 1f, "La progressione non deve mai superare 1.");
    }

    // ─────────────────────────────────────────────────────────────
    // Soglia di Completamento
    // ─────────────────────────────────────────────────────────────

    [Test]
    public void SogliaCompletamento_ProgressioneSupera_Completamento()
    {
        float soglia     = 0.70f;
        float progresso  = 0.75f;
        bool completato  = progresso >= soglia;

        Assert.IsTrue(completato, "Con progresso 75% e soglia 70% la fase deve risultare completata.");
    }

    [Test]
    public void SogliaCompletamento_ProgressioneInferiore_NonCompletamento()
    {
        float soglia    = 0.70f;
        float progresso = 0.60f;
        bool completato = progresso >= soglia;

        Assert.IsFalse(completato, "Con progresso 60% e soglia 70% la fase non deve risultare completata.");
    }

    [Test]
    public void SogliaCompletamento_ProgressioneUguale_Completamento()
    {
        float soglia    = 0.70f;
        float progresso = 0.70f;
        bool completato = progresso >= soglia;

        Assert.IsTrue(completato, "Con progresso esattamente pari alla soglia la fase deve risultare completata.");
    }

    // ─────────────────────────────────────────────────────────────
    // Wrap UV (RestorationUtils.WrapUV)
    // ─────────────────────────────────────────────────────────────

    [Test]
    public void WrapUV_ValoreNormale_Invariato()
    {
        Vector2 uv = new Vector2(0.5f, 0.5f);
        Vector2 wrapped = RestorationUtils.WrapUV(uv);
    
        Assert.AreEqual(0.5f, wrapped.x, 0.001f);
        Assert.AreEqual(0.5f, wrapped.y, 0.001f);
    }

    [Test]
    public void WrapUV_ValoreSupera1_TornaInRange()
    {
        Vector2 uv = new Vector2(1.3f, 0.8f);
        Vector2 wrapped = RestorationUtils.WrapUV(uv);

        Assert.GreaterOrEqual(wrapped.x, 0f);
        Assert.Less(wrapped.x, 1f);
    }

    [Test]
    public void WrapUV_ValoreNegativo_TornaInRange()
    {
        Vector2 uv = new Vector2(-0.2f, 0.5f);
        Vector2 wrapped = RestorationUtils.WrapUV(uv);

        Assert.GreaterOrEqual(wrapped.x, 0f);
        Assert.Less(wrapped.x, 1f);
    }
}
