using PlayerTags.Features;
using System.Text;

var assertions = 0;
void Equal<T>(T expected, T actual)
{
    assertions++;
    if (!Equals(expected, actual)) throw new Exception($"Expected {expected}; got {actual}");
}
var rules = new UiNameRules();
rules.Add("Alpha Beta", "White Mage"); rules.Add("测试角色", "白魔法师"); rules.Seal();
Equal("White Mage", rules.Replace("Alpha Beta"));
Equal("White Mage", rules.Replace("A. Beta"));
Equal("White Mage", rules.Replace("Alpha B."));
Equal("1Lv80 White Mage", rules.Replace("1Lv80 A. B."));
Equal("白魔法师", rules.Replace("测试角色"));
Equal("OtherAlpha Beta", rules.Replace("OtherAlpha Beta"));
Equal("Alpha Betaman", rules.Replace("Alpha Betaman"));
Equal("Seraph", rules.Replace("Seraph"));
var ambiguous = new UiNameRules();
ambiguous.Add("Alpha Beta", "White Mage"); ambiguous.Add("Amber Blue", "Paladin"); ambiguous.Seal();
Equal("A. B.", ambiguous.Replace("A. B."));
Equal("White Mage", ambiguous.Replace("Alpha Beta"));
var sameJob = new UiNameRules();
sameJob.Add("Alpha Beta", "White Mage"); sameJob.Add("Amber Blue", "White Mage"); sameJob.Seal();
Equal("White Mage", sameJob.Replace("A. B."));
var combined = new UiNameRules();
combined.Add("Alpha Beta", "White Mage"); combined.Exclude("Amber Blue"); combined.Seal();
Equal("A. B.", combined.Replace("A. B."));
Equal("White Mage", combined.Replace("Alpha Beta"));
Equal("Amber Blue", combined.Replace("Amber Blue"));
var both = new UiNameRules();
both.Add("Alpha Beta", "Paladin"); both.Add("Charlie Delta", "White Mage"); both.Seal();
Equal("Paladin", both.Replace("Alpha Beta"));
Equal("White Mage", both.Replace("Charlie Delta"));
var primaryOnly = new UiNameRules();
primaryOnly.Add("Alpha Beta", "Paladin"); primaryOnly.Exclude("Charlie Delta"); primaryOnly.Seal();
Equal("Paladin", primaryOnly.Replace("A. B."));
Equal("C. D.", primaryOnly.Replace("C. D."));
var snapshot = new UiTextSnapshot(Encoding.UTF8.GetBytes("Alpha Beta"), Encoding.UTF8.GetBytes("White Mage"));
Equal(true, snapshot.StillOurs(Encoding.UTF8.GetBytes("White Mage")));
Equal("Alpha Beta", Encoding.UTF8.GetString(snapshot.SourceFor(Encoding.UTF8.GetBytes("White Mage"))));
Equal(false, snapshot.StillOurs(Encoding.UTF8.GetBytes("New Player")));
Equal("New Player", Encoding.UTF8.GetString(snapshot.SourceFor(Encoding.UTF8.GetBytes("New Player"))));
Equal("White Mage", Encoding.UTF8.GetString(UiTextSnapshot.TextBytes(Encoding.UTF8.GetBytes("White Mage\0"))));
Equal("White Mage", Encoding.UTF8.GetString(UiTextSnapshot.TextBytes(Encoding.UTF8.GetBytes("White Mage"))));
Equal(true, snapshot.StillOurs(UiTextSnapshot.TextBytes(Encoding.UTF8.GetBytes("White Mage\0"))));
Console.WriteLine($"Passed {assertions} assertions: full/short names, Chinese, ambiguous initials, target isolation, and safe restore.");
