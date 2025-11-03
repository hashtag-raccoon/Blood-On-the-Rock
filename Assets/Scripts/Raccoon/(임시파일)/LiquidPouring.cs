using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LiquidPouring : MonoBehaviour
{

    [Space(2)]
    [Header("기주_오브젝트 관리")]
    [SerializeField] private GameObject drinkObject;
    [SerializeField, Range(1f, 100f)] private float rotationSpeed = 75f;
    [SerializeField, Range(0.1f, 3f)] private float rotationSensitivity = 2f;
    [SerializeField] private float maxRotation = -135f;

    [Space(2)]
    [Header("액체 붓기 시스템")]
    [SerializeField] private Transform pourPoint;
    [SerializeField] private ParticleSystem pourEffect;  // 파티클 연결
    [SerializeField] private float maxCocktailAmount = 100f;
    [SerializeField] private float currentCocktailAmount = 0f;
    [SerializeField] private GameObject liquidFill;
    [SerializeField] private LiquidSensor sensorA;
    private bool isDragging = false;
    private Collider2D drinkCollider;

    private float lastMouseY;
    private float currentRotation;

    // 각도에 따른 파티클 제어값
    private float pourStartAngle = -30f;  // 이 각도 이상 기울이면 술이 흐름 시작
    private float maxPourAngle = -120f;   // 완전히 기울었을 때

    private ParticleSystem.EmissionModule emission;
    // Start is called before the first frame update
    private void Awake()
    {
        drinkCollider = drinkObject.GetComponent<Collider2D>();
        emission = pourEffect.emission;
        emission.rateOverTime = 0;  // 시작 시 멈춘 상태
    }
    private void Start()
    {
        // 센서 이벤트 등록
        sensorA.onEnter += LiquidEnter;
        sensorA.onExit += LiquidExit;
    }

    // Update is called once per frame
    private void Update()
    {

        // 마우스 클릭 시작
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity);

            if (hit.collider == drinkCollider)
            {
                isDragging = true;
                lastMouseY = Input.mousePosition.y;
            }
        }

        // 마우스 클릭 해제
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging)
        {
            float mouseY = Input.mousePosition.y;
            float deltaY = mouseY - lastMouseY;
            lastMouseY = mouseY;

            float rotationDelta = deltaY * rotationSensitivity;
            currentRotation += rotationDelta * Time.deltaTime * rotationSpeed;
            currentRotation = Mathf.Clamp(currentRotation, maxRotation, 0f);

            drinkObject.transform.rotation = Quaternion.Euler(0, 0, currentRotation);
        }
        else
        {
            if (Mathf.Abs(currentRotation) > 0.1f)
            {
                currentRotation = Mathf.MoveTowards(currentRotation, 0f, rotationSpeed * Time.deltaTime);
                drinkObject.transform.rotation = Quaternion.Euler(0, 0, currentRotation);
            }
            else
            {
                currentRotation = 0f;
                drinkObject.transform.rotation = Quaternion.Euler(0, 0, 0);
            }
        }

        // 파티클 붓기 제어
        HandlePouringEffect();
        // ui 게이지 증가
        liquidFill.GetComponent<Image>().fillAmount = (float)currentCocktailAmount / maxCocktailAmount;
    }


    private void HandlePouringEffect()
    {
        // 일정 각도 이상 기울이면 술이 흐르기 시작
        if (currentRotation <= pourStartAngle)
        {
            if (!pourEffect.isPlaying)
            {
                pourEffect.Play();
            }

            // 각도 비율에 따라 파티클 양 조절
            float t = Mathf.InverseLerp(pourStartAngle, maxPourAngle, currentRotation);
            float rate = Mathf.Lerp(5f, 50f, t); // 최소 5, 최대 50 입자/초
            emission.rateOverTime = rate;
        }
        else
        {
            if (pourEffect.isPlaying)
            {
                pourEffect.Stop();
            }
        }
    }

    public void LiquidEnter(GameObject cup)
    {
        currentCocktailAmount = Mathf.Min(currentCocktailAmount + 1f, maxCocktailAmount);
        // 파티클도 여기서 지우는 방법 가능
        if (pourEffect.isPlaying)
        {
            pourEffect.Clear();
        }
    }


    public void LiquidExit(GameObject Liquid)
    {

    }

    private void OnDestroy()
    {
        //메모리 누수 방지
        sensorA.onEnter -= LiquidEnter;
        sensorA.onExit -= LiquidExit;
    }
}
