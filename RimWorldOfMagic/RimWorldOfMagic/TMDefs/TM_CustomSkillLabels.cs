using System.Collections.Generic;

namespace TorannMagic.TMDefs
{
    // Exact historical aliases from the removed Russian custom-skill label injections.
    // These are save identifiers, independent of the active display language.
    internal static class TM_CustomSkillLabels
    {
        private static readonly Dictionary<string, string> LegacyRussian = new Dictionary<string, string>
        {
            { "Соперник", "TM_Taunt_pwr" },
            { "Книга оскорблений", "TM_Taunt_eff" },
            { "Ревущий рык", "TM_Taunt_ver" },
            { "Разрушение", "TM_Explosion_pwr" },
            { "Песнопение", "TM_Explosion_eff" },
            { "Улучшение зрения", "TM_Explosion_ver" },
            { "Вместимость", "TM_MedicalSupply_ver" },
            { "Боевой медик", "TM_Medigel_eff" },
            { "Эффективность", "TM_CombatDrugs_pwr" },
            { "Стабилизация", "TM_CombatDrugs_ver" },
            { "Эффективность щита", "TM_Shield_eff" },
            { "Ментальная перегрузка", "TM_Discord_eff" },
            { "Позитивное мышление", "TM_Empathy_pwr" },
            { "Открытая книга", "TM_Empathy_ver" },
            { "Групповая терапия", "TM_Empathy_eff" },
            { "Удушье", "TM_MindKiller_pwr" },
            { "Борьба со штормом", "TM_MindKiller_ver" },
            { "Стальной Взгляд", "TM_MindKiller_eff" },
            { "Затянувшееся Подавление", "TM_SuppressiveAura_pwr" },
            { "Блаженная Ложь", "TM_SuppressiveAura_ver" },
            { "Огромная Аура", "TM_SuppressiveAura_eff" },
            { "Страстный охотник", "TM_HarvestPassion_pwr" },
            { "Отчаяние", "TM_HarvestPassion_ver" },
            { "Хулиган", "TM_HarvestPassion_eff" },
            { "Осведомленность", "TM_Symbiosis_pwr" },
            { "Коммуна", "TM_Symbiosis_eff" },
            { "Ракушка", "TM_Symbiosis_ver" },
            { "Эффективность ингредиентов", "TM_FireSuppressionFlask_eff" },
            { "Сила духа", "TM_SpiritPossession_pwr" },
            { "Контроль духа", "TM_SpiritPossession_eff" },
            { "Управление духом", "TM_SpiritPossession_ver" },
            { "Устранение повреждений", "TM_SpiritDrain_pwr" },
            { "Чистая энергия", "TM_SpiritDrain_eff" },
            { "Быстрое поглощение", "TM_SpiritDrain_ver" },
            { "Интенсивность", "TM_DistortSpirit_pwr" },
            { "Контроль искажения", "TM_DistortSpirit_eff" },
            { "Разрывы духа", "TM_DistortSpirit_ver" },
            { "Проявление", "TM_SummonSpiritAnimalMass_pwr" },
            { "Сородичи", "TM_SummonSpiritAnimalMass_eff" },
            { "Гармония", "TM_SummonSpiritAnimalMass_ver" },
            { "Армия духов", "TM_SpiritRally_pwr" },
            { "Общее восстановление", "TM_SpiritRally_eff" },
            { "Влияние", "TM_SpiritRally_ver" },
            { "Бешенство духа", "TM_SpiritStorm_pwr" },
            { "Сочувствие", "TM_SpiritStorm_eff" },
            { "Море печали", "TM_SpiritStorm_ver" },
            { "Жадное Потребление", "TM_ConsumeCorpse_eff" },
            { "Целебное Потребление", "TM_ConsumeCorpse_ver" },
        };

        internal static string Canonical(string label)
        {
            string canonical;
            return label != null && LegacyRussian.TryGetValue(label, out canonical) ? canonical : label;
        }
    }
}
