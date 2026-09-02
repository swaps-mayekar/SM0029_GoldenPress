using System.Collections;
using System.IO;
using GoldenPress.Core;
using GoldenPress.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GoldenPress.Tests
{
    public class PlayModeSmokeTests
    {
        [UnityTest]
        public IEnumerator SplashScene_Loads_AndContainsEntryPoint()
        {
            var op = SceneManager.LoadSceneAsync(SceneNames.Splash, LoadSceneMode.Single);
            while (!op.isDone)
            {
                yield return null;
            }

            Assert.AreEqual(SceneNames.Splash, SceneManager.GetActiveScene().name);
            yield return null;
            Assert.IsNotNull(Object.FindFirstObjectByType<GoldenPress.Bootstrap.SceneEntryPoint>());
        }

        [UnityTest]
        public IEnumerator ProductionPipeline_ScoresAdvanceStages()
        {
            var path = Path.Combine(Application.temporaryCachePath, "gp_play_" + Path.GetRandomFileName() + ".json");
            var balance = GameBalanceConfig.CreateDefault();
            var session = new GameSession(balance, new SaveService(path));
            session.State.money = 500;
            Assert.IsTrue(session.Production.TryBeginForCurrentOrder().Success);
            Assert.AreEqual(ProductionStage.Sorting, session.Production.Session.currentStage);

            session.Production.SetStageScore(ProductionStage.Sorting, 0.8f);
            Assert.AreEqual(ProductionStage.Processing, session.Production.Session.currentStage);

            session.Production.SetStageScore(ProductionStage.Processing, 0.7f);
            Assert.AreEqual(ProductionStage.Bottling, session.Production.Session.currentStage);

            session.Production.SetStageScore(ProductionStage.Bottling, 0.9f);
            Assert.IsTrue(session.State.GetOilLiters(OilIds.Groundnut) >= balance.tutorialOrderLiters - 0.01f);
            Assert.AreEqual(TutorialStep.FulfillOrder, session.Tutorial.CurrentStep);

            if (File.Exists(path)) File.Delete(path);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SaveResume_RestoresTutorialStep()
        {
            var path = Path.Combine(Application.temporaryCachePath, "gp_resume_" + Path.GetRandomFileName() + ".json");
            var balance = GameBalanceConfig.CreateDefault();
            var session = new GameSession(balance, new SaveService(path));
            session.Tutorial.AdvanceTo(TutorialStep.PurchaseMaterials);
            session.SaveService.Save();

            var resumed = new GameSession(balance, new SaveService(path));
            Assert.AreEqual(TutorialStep.PurchaseMaterials, resumed.Tutorial.CurrentStep);
            if (File.Exists(path)) File.Delete(path);
            yield return null;
        }
    }
}
