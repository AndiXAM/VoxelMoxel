using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Dash Thrust", menuName = "RPG/Skills/Dash Thrust")]
public class DashThrustSkill : SkillItem
{
    [Header("1. Тайминги Анимации (под 60 FPS)")]
    [Tooltip("Кадры 0-27: стойка с прицеливанием камеры (0.45 сек)")]
    public float prepDuration = 0.45f;

    [Tooltip("Кадры 27-65: максимальный резкий выпад вперед (0.6 сек)")]
    public float thrustDuration = 0.6f;

    [Tooltip("Кадры 65-105: первая фаза торможения ЕЩЕ В АНИМАЦИИ (0.4 сек)")]
    public float inAnimDecelDuration = 0.4f;

    [Header("2. Доскальзывание после анимации (WASD уже работает)")]
    [Tooltip("Длительность финального затухания ПОСЛЕ анимации")]
    public float postInertiaDuration = 0.25f;

    [Header("3. Настройки Поворота и Прицеливания")]
    [Tooltip("Скорость поворота во время стойки подготовки (быстрый прицел)")]
    public float prepTurnSpeed = 16.0f;

    [Tooltip("Скорость медленного доворота ВО ВРЕМЯ РЫВКА (1.5 - 2.5 = около 10 градусов за весь выпад)")]
    public float dashTurnSpeed = 2.0f;

    [Header("4. Настройки Дистанции и Урона")]
    public float thrustDistance = 3.5f;
    public float damage = 10f;
    public float skillImpactPower = 1.5f;
    public Vector3 hitboxSize = new Vector3(1.5f, 1.5f, 2.2f);
    public Vector3 hitboxOffset = new Vector3(0, 1.0f, 1.3f);

    public override void ExecuteSkill(PlayerSkillHandler user, Weapon weaponFromSlot)
    {
        user.StartCoroutine(PerformDashThrustRoutine(user, weaponFromSlot));
    }

    private IEnumerator PerformDashThrustRoutine(PlayerSkillHandler user, Weapon weaponFromSlot)
    {
        // 1. Надежно находим CharacterController и саму Капсулу
        CharacterController cc = user.GetComponent<CharacterController>();
        if (cc == null) cc = user.GetComponentInParent<CharacterController>();
        Transform capsule = cc != null ? cc.transform : user.transform;

        StatsContainer stats = user.playerStats;
        
        // 2. берем камеру из игрока
        Transform cam = user.playerCamera;

        float finalDamage = damage + (weaponFromSlot != null ? weaponFromSlot.damage : 0);

        // Блокируем WASD
        if (stats != null) stats.IsMovementLocked = true;

        // ================= ФАЗА 1: СТОЙКА И ПРИЦЕЛИВАНИЕ =================
        float timer = 0f;
        while (timer < prepDuration)
        {
            timer += Time.deltaTime;

            // Быстрый поворот капсулы в сторону взгляда камеры
            ApplyRotation(capsule, cam, prepTurnSpeed);

            yield return null;
        }

        // ================= ФАЗА 2: ФИКСАЦИЯ НАПРАВЛЕНИЯ И РЫВОК =================
        // Фиксируем физический вектор полета строго по текущему взгляду капсулы
        Vector3 initialDashDir = capsule.forward;
        initialDashDir.y = 0f;
        initialDashDir.Normalize();

        if (user.audioSource != null && activationSound != null)
            user.audioSource.PlayOneShot(activationSound);

        float startSpeed = (thrustDistance / thrustDuration) * 1.3f;
        HashSet<GameObject> hitTargets = new HashSet<GameObject>();

        // 2.1 Активный полет вперед
        timer = 0f;
        while (timer < thrustDuration)
        {
            timer += Time.deltaTime;

            if (cc != null)
            {
                Vector3 move = initialDashDir * startSpeed + Vector3.down * 9.81f;
                cc.Move(move * Time.deltaTime);
            }

            // МИКРО-ДОВОРОТ: очень медленно смещаем взгляд капсулы за камерой
            ApplyRotation(capsule, cam, dashTurnSpeed);

            CheckHitbox(user, capsule, initialDashDir, finalDamage, hitTargets);
            yield return null;
        }

        // 2.2 Первая половина торможения (ВНУТРИ АНИМАЦИИ)
        timer = 0f;
        while (timer < inAnimDecelDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / inAnimDecelDuration;

            float currentSpeed = Mathf.Lerp(startSpeed * 0.7f, 2.0f, progress);

            if (cc != null)
            {
                Vector3 move = initialDashDir * currentSpeed + Vector3.down * 9.81f;
                cc.Move(move * Time.deltaTime);
            }

            // Корпус продолжает плавно и медленно подруливать
            ApplyRotation(capsule, cam, dashTurnSpeed);

            yield return null;
        }

        // ================= ФАЗА 3: ВОЗВРАТ УПРАВЛЕНИЯ + ВТОРАЯ ПОЛОВИНА ЗАТУХАНИЯ =================
        if (stats != null) stats.IsMovementLocked = false;

        timer = 0f;
        while (timer < postInertiaDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / postInertiaDuration;

            float residualSpeed = Mathf.Lerp(2.0f, 0f, progress);

            if (cc != null && residualSpeed > 0.05f)
            {
                cc.Move(initialDashDir * residualSpeed * Time.deltaTime);
            }

            yield return null;
        }
    }

    // Вспомогательный метод поворота (работает аналогично ThirdPersonCam)
    private void ApplyRotation(Transform capsule, Transform cam, float turnSpeed)
    {
        if (cam == null || turnSpeed <= 0f) return;

        Vector3 cameraForward = cam.forward;
        cameraForward.y = 0f;

        if (cameraForward.sqrMagnitude > 0.001f)
        {
            capsule.forward = Vector3.Slerp(capsule.forward, cameraForward.normalized, Time.deltaTime * turnSpeed);
        }
    }

    private void CheckHitbox(PlayerSkillHandler user, Transform capsule, Vector3 attackDir, float dmg, HashSet<GameObject> hitTargets)
    {
        Vector3 center = capsule.position + capsule.TransformDirection(hitboxOffset);
        Collider[] hits = Physics.OverlapBox(center, hitboxSize * 0.5f, capsule.rotation);

        foreach (var col in hits)
        {
            EnemyHealthSystem enemyHealth = col.GetComponentInParent<EnemyHealthSystem>();
            if (enemyHealth != null && !hitTargets.Contains(enemyHealth.gameObject))
            {
                hitTargets.Add(enemyHealth.gameObject);
                enemyHealth.GetDamage(Mathf.RoundToInt(dmg));

                // Играем звук попадания из поля impactSound базового класса
                if (user != null && user.audioSource != null && impactSound != null)
                    user.audioSource.PlayOneShot(impactSound);

                StatsContainer enemyStats = enemyHealth.GetComponentInParent<StatsContainer>();
                if (enemyStats != null)
                {
                    enemyStats.TakeImpact(skillImpactPower, enemyStats.IsBlock, attackDir);
                }
            }
        }
    }
}