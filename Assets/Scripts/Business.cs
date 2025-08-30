using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

public class Business : MonoBehaviour
{
    // Variable estática global para controlar QTE activo
    private static bool globalQTEActive = false;
    
    [Header("Config")]
    public string businessName;
    public double baseCost = 10;
    public double baseProfit = 1;   // 👈 ahora es el profit base
    public float productionTime = 2f; // en segundos

    [Header("Quick Time Event - NUEVO SISTEMA")]
    [Range(0f, 1f)]
    public float qteChance = 0.3f; // 30% de probabilidad de QTE (ya no se usa)
    public float qteTimeLimit = 3f; // tiempo límite para resolver el QTE
    public KeyCode qteKey = KeyCode.Space; // tecla para resolver el QTE

    [Header("Estado")]
    public int level = 0;
    private bool isRunning = false;
    private bool isQTEActive = false;
    private float qteTimer = 0f;
    private bool isPenalized = false; // Nueva variable para penalización
    private float penaltyTimer = 0f; // Timer para la penalización

    [Header("UI References (TMP)")]
    public TMP_Text nameText;
    public TMP_Text levelText;
    public TMP_Text costText;
    public TMP_Text profitText;
    public Button buyButton;
    public Button produceButton;  // 👈 botón manual (nivel < 10)
    public Image progressBar;
    
    [Header("QTE UI")]
    public GameObject qtePanel; // Panel para mostrar el QTE
    public TMP_Text qteText; // Texto del QTE
    public Image qteProgressBar; // Barra de progreso del QTE
    public Button qteTestButton; // Botón de prueba para el QTE
    public Sprite[] qteGifFrames; // 👈 NUEVO: Frames del GIF
    public float qteGifFrameRate = 12f; // 👈 NUEVO: Velocidad de reproducción
    
    [Header("Penalización UI")]
    public Image penaltyProgressBar; // Barra de progreso de la penalización

    private void Start()
    {
        nameText.text = businessName;
        buyButton.onClick.AddListener(Buy);
        produceButton.onClick.AddListener(StartProduction); // 👈 acción manual
        
        // Configurar botón de prueba QTE
        if (qteTestButton != null)
        {
            qteTestButton.onClick.AddListener(TestQTE);
            qteTestButton.gameObject.SetActive(false); // Oculto por defecto
        }
        
        UpdateUI();
        
        // Ocultar panel de QTE al inicio
        if (qtePanel != null)
            qtePanel.SetActive(false);
            
        // Resetear barra de penalización al inicio
        if (penaltyProgressBar != null)
            penaltyProgressBar.fillAmount = 0f;
    }

    void Update()
    {
        UpdateUI();

        // 👇 Si tiene nivel >= 10 y no está produciendo, no hay QTE activo, no hay penalización Y no está corriendo
        if (level >= 10 && !isRunning && !isQTEActive && !isPenalized)
        {
            StartCoroutine(ProduceOnce());
        }

        // Manejo del Quick Time Event
        if (isQTEActive)
        {
            HandleQTE();
        }
        
        // Manejo de la penalización
        if (isPenalized)
        {
            HandlePenalty();
        }
        
        // Mostrar botón de prueba cuando llegue a nivel 10
        if (level >= 10 && qteTestButton != null)
        {
            qteTestButton.gameObject.SetActive(true);
        }
    }

    // NUEVO SISTEMA QTE - Lógica simplificada
    private void HandleQTE()
    {
        // Actualizar timer
        qteTimer += Time.deltaTime;
        
        // Actualizar barra de progreso del QTE
        if (qteProgressBar != null)
        {
            qteProgressBar.fillAmount = qteTimer / qteTimeLimit;
        }
        
        // DETECCIÓN DE TECLA SIMPLIFICADA - Usar Input.GetKeyDown del Input System antiguo
        if (Input.GetKeyDown(qteKey))
        {
            Debug.Log($"¡SPACE presionado! QTE exitoso en {businessName}");
            QTESuccess();
            return;
        }
        
        // Verificar si se agotó el tiempo
        if (qteTimer >= qteTimeLimit)
        {
            Debug.Log($"¡Tiempo agotado! QTE fallido en {businessName}");
            QTEFail();
        }
    }

    private void QTESuccess()
    {
        double qteBonus = GetProfit() * 1.15; // Bonus del 15% sobre el profit base
        Debug.Log($"¡QTE EXITOSO en {businessName}! Bonus: +${qteBonus:F2} (profit base: ${GetProfit():F2} × 1.15)");
        
        // Bonus por QTE exitoso (15% sobre el profit base)
        GameManager.instance.money += qteBonus;
        
        // Limpiar estado del QTE
        CleanupQTE();
        
        // Continuar con la producción automática
        if (level >= 10)
        {
            StartCoroutine(ProduceOnce());
        }
    }

    private void QTEFail()
    {
        Debug.Log($"¡QTE FALLIDO en {businessName}! Sin bonus. Penalización activada.");
        
        // Limpiar estado del QTE
        CleanupQTE();
        
        // ACTIVAR PENALIZACIÓN - Frenar producción durante productionTime × 10
        isPenalized = true;
        penaltyTimer = 0f;
        float penaltyDuration = productionTime * 10f;
        
        // Resetear barra de progreso de penalización
        if (penaltyProgressBar != null)
        {
            penaltyProgressBar.fillAmount = 0f;
        }
        
        Debug.Log($"Penalización activada en {businessName} - Duración: {penaltyDuration:F1} segundos");
        
        // NO continuar con la producción automática hasta que termine la penalización
        // La producción se reanudará automáticamente en Update() cuando isPenalized = false
    }

    // NUEVA FUNCIÓN - Limpiar estado del QTE
    private void CleanupQTE()
    {
        isQTEActive = false;
        globalQTEActive = false; // Liberar el QTE global
        qteTimer = 0f;
        
        // Ocultar panel de QTE con animación
        if (qtePanel != null)
        {
            StartCoroutine(AnimateQTEExit());
        }
        
        // Resetear barra de progreso
        if (qteProgressBar != null)
        {
            qteProgressBar.fillAmount = 0f;
        }
        
        Debug.Log($"QTE limpiado en {businessName} - Global liberado");
    }

    // NUEVA FUNCIÓN - Calcular probabilidad dinámica del QTE
    private float GetDynamicQTEChance()
    {
        if (level < 10)
        {
            return 0f; // No hay QTE antes del nivel 10
        }
        
        // Fórmula: 1% base + 1.25% cada 4 niveles después del nivel 10
        int levelsAfter10 = level - 10;
        int bonusLevels = levelsAfter10 / 4; // Cada 4 niveles
        float bonusChance = bonusLevels * 0.0150f; // 1.5% = 0.015
        
        float finalChance = 0.01f + bonusChance; // 1% base + bonus
        
        // Limitar a máximo 100%
        finalChance = Mathf.Min(finalChance, 1f);
        
        return finalChance;
    }

    // NUEVO SISTEMA QTE - Lanzamiento simplificado
    private void LaunchQTE()
    {
        // Solo lanzar QTE si es nivel 10+, no hay uno activo localmente Y globalmente, Y no hay penalización
        if (level >= 10 && !isQTEActive && !globalQTEActive && !isPenalized)
        {
            // Obtener probabilidad dinámica
            float currentQTEChance = GetDynamicQTEChance();
            
            // Verificar probabilidad
            if (UnityEngine.Random.Range(0f, 1f) <= currentQTEChance)
            {
                Debug.Log($"¡QTE INICIADO en {businessName}! Probabilidad: {currentQTEChance * 100:F2}%");
                
                // Activar QTE local y global
                isQTEActive = true;
                globalQTEActive = true;
                qteTimer = 0f;
                
                // Mostrar UI del QTE con animación
                if (qtePanel != null)
                {
                    qtePanel.SetActive(true);
                    StartCoroutine(AnimateQTEEnter());
                    // Iniciar reproducción del GIF
                    if (qteGifFrames.Length > 0)
                    {
                        StartCoroutine(PlayQTEAnimation());
                    }
                }
                
                if (qteText != null)
                {
                    qteText.text = $"RÁPIDO!";
                }
                
                if (qteProgressBar != null)
                {
                    qteProgressBar.fillAmount = 0f;
                }
            }
            else
            {
                // No hay QTE, continuar producción
                Debug.Log($"No hay QTE en {businessName} (prob: {currentQTEChance * 100:F2}%), continuando producción...");
                if (level >= 10)
                {
                    StartCoroutine(ProduceOnce());
                }
            }
        }
        else
        {
            // No se puede lanzar QTE, continuar producción solo si no hay penalización
            if (level >= 10 && !isPenalized)
            {
                Debug.Log($"QTE no disponible en {businessName} - Local: {isQTEActive}, Global: {globalQTEActive}, Penalizado: {isPenalized}");
                StartCoroutine(ProduceOnce());
            }
            else if (isPenalized)
            {
                Debug.Log($"{businessName} está penalizado - No se puede producir hasta que termine la penalización");
            }
        }
    }

    public void Buy()
    {
        double cost = GetCost();
        if (GameManager.instance.money >= cost)
        {
            GameManager.instance.money -= cost;
            level++;
            UpdateUI();
        }
    }

    private void StartProduction()
    {
        // 👇 Solo manual si nivel < 10
        if (level > 0 && !isRunning && level < 10)
        {
            StartCoroutine(ProduceOnce());
        }
    }

    private IEnumerator ProduceOnce()
    {
        isRunning = true;
        produceButton.interactable = false; // 👈 deshabilitamos hasta que termine

        float timer = 0;
        while (timer < productionTime)
        {
            timer += Time.deltaTime;
            if (progressBar != null)
                progressBar.fillAmount = timer / productionTime;
            yield return null;
        }

        // 🚀 ganancia por producción
        GameManager.instance.money += GetProfit();

        if (progressBar != null)
            progressBar.fillAmount = 0;

        isRunning = false;

        // 👇 reactivar el botón solo si es manual
        if (level < 10)
            produceButton.interactable = true;
        else
            produceButton.interactable = false; // a nivel 10+ no hace falta

        // Verificar si se debe lanzar un Quick Time Event
        LaunchQTE();
    }

 // Fórmula mixta: combina diferentes tipos de crecimiento
public double GetCost()
{
    // NUEVA FÓRMULA: (costo inicial + profit) × level + baseCost
    // Agregamos baseCost para evitar que el costo sea 0 cuando level es 0
    return ((baseCost + baseProfit) * level) + baseCost;
}

public double GetProfit()
{
    // NUEVA FÓRMULA: (profit base) × level + bonificaciones por hitos
    // Si level es 0, retornar 0 para evitar cálculos incorrectos
    if (level <= 0) return 0;
    
    double baseProfitValue = baseProfit * level;
    
    // Bonificaciones por hitos específicos
    if (level >= 100) baseProfitValue *= 4.0; // +300% bonus nivel 100+
    else if (level >= 75) baseProfitValue *= 3.5; // +250% bonus nivel 75+
    else if (level >= 50) baseProfitValue *= 3.0; // +200% bonus nivel 50+
    else if (level >= 25) baseProfitValue *= 2.0; // +100% bonus nivel 25+
    else if (level >= 10) baseProfitValue *= 1.5; // +50% bonus nivel 10+
    
    return baseProfitValue;
}

    private void UpdateUI()
    {
        levelText.text = "Lvl: " + level;
        costText.text = "$" + GetCost().ToString("F0");
        profitText.text = "$" + GetProfit().ToString("F0") + " / " + productionTime + "s";
        buyButton.interactable = GameManager.instance.money >= GetCost();

        // 👇 Manual disponible solo antes de nivel 10
        produceButton.interactable = (level > 0 && !isRunning && level < 10);
    }

    // Botón de prueba para el QTE
    private void TestQTE()
    {
        Debug.Log($"Botón de prueba QTE presionado en {businessName}");
        
        if (!isQTEActive && !isRunning)
        {
            Debug.Log("Iniciando QTE de prueba...");
            LaunchQTE();
        }
        else
        {
            Debug.Log($"No se puede iniciar QTE - isQTEActive: {isQTEActive}, isRunning: {isRunning}");
        }
    }

    // NUEVA FUNCIÓN - Manejar la penalización
    private void HandlePenalty()
    {
        penaltyTimer += Time.deltaTime;
        float penaltyDuration = productionTime * 10f;
        
        // Actualizar barra de progreso de la penalización
        if (penaltyProgressBar != null)
        {
            penaltyProgressBar.fillAmount = 1f - (penaltyTimer / penaltyDuration);
        }
        
        // Verificar si la penalización terminó
        if (penaltyTimer >= penaltyDuration)
        {
            Debug.Log($"Penalización terminada en {businessName} - Producción reanudada");
            isPenalized = false;
            penaltyTimer = 0f;
            
            // Resetear barra de progreso de penalización
            if (penaltyProgressBar != null)
            {
                penaltyProgressBar.fillAmount = 0f;
            }
        }
        else
        {
            // Mostrar tiempo restante de penalización
            float remainingTime = penaltyDuration - penaltyTimer;
            Debug.Log($"Penalización activa en {businessName} - Tiempo restante: {remainingTime:F1}s");
        }
    }

    // NUEVA FUNCIÓN: Animación de entrada del QTE
    private IEnumerator AnimateQTEEnter()
    {
        // Configurar escala inicial
        qtePanel.transform.localScale = Vector3.zero;
        
        // Animación de entrada suave
        float duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            float scale = Mathf.SmoothStep(0f, 1f, progress);
            qtePanel.transform.localScale = new Vector3(scale, scale, scale);
            yield return null;
        }
        
        // Asegurar escala final
        qtePanel.transform.localScale = Vector3.one;
    }

    // NUEVA FUNCIÓN: Animación de salida del QTE
    private IEnumerator AnimateQTEExit()
    {
        // Animación de salida suave
        float duration = 0.2f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            float scale = Mathf.SmoothStep(1f, 0f, progress);
            qtePanel.transform.localScale = new Vector3(scale, scale, scale);
            yield return null;
        }
        
        // Ocultar el panel
        qtePanel.SetActive(false);
        
        // Resetear escala
        qtePanel.transform.localScale = Vector3.one;
    }

    // NUEVA FUNCIÓN: Reproducir animación del GIF en el panel
    private IEnumerator PlayQTEAnimation()
    {
        if (qtePanel == null || qteGifFrames.Length == 0) yield break;
        
        int currentFrame = 0;
        float frameDelay = 1f / qteGifFrameRate;
        
        // Obtener la imagen del panel (asumiendo que es el primer Image component)
        Image panelImage = qtePanel.GetComponent<Image>();
        if (panelImage == null)
        {
            // Si no hay Image component en el panel, buscar en los hijos
            panelImage = qtePanel.GetComponentInChildren<Image>();
        }
        
        if (panelImage == null) yield break;
        
        while (qtePanel.activeInHierarchy)
        {
            // Cambiar frame del panel
            panelImage.sprite = qteGifFrames[currentFrame];
            
            // Siguiente frame
            currentFrame = (currentFrame + 1) % qteGifFrames.Length;
            
            // Esperar
            yield return new WaitForSeconds(frameDelay);
        }
    }
}
