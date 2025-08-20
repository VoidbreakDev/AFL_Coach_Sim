using NUnit.Framework;
using AFLCoachSim.Core.Domain.Entities;
using AFLCoachSim.Core.Domain.ValueObjects;

namespace AFLCoachSim.Core.Tests
{
    public class M1_PlayerRoleFitTests
    {
        [Test]
        public void Attributes_Are_Independent()
        {
            var p = new Player
            {
                Id = new PlayerId(1),
                Name = "Test",
                PrimaryRole = Role.MID,
                Attr = new Attributes { Clearance = 80, Kicking = 60, WorkRate = 70 }
            };

            Assert.That(p.Attr.Clearance, Is.EqualTo(80));
            Assert.That(p.Attr.Kicking, Is.EqualTo(60));
            Assert.That(p.Attr.WorkRate, Is.EqualTo(70));
        }
    }
}