////The implementation is based on this article:http://rbarraza.com/html5-canvas-pageflip/
////As the rbarraza.com website is not live anymore you can get an archived version from web archive 
////or check an archived version that I uploaded on my website: https://dandarawy.com/html5-canvas-pageflip/

//using UnityEngine;
//using System.Collections;
//using UnityEngine.UI;
//using UnityEngine.Events;
//public enum FlipMode
//{
//    RightToLeft,
//    LeftToRight
//}
//[ExecuteInEditMode]
//public class Book : MonoBehaviour
//{
//    public Canvas canvas;
//    [SerializeField]
//    RectTransform BookPanel;
//    public Sprite background;
//    public Sprite[] bookPages;
//    public GameObject[] stepPanels;  // Each step is a UI prefab with text/image/layout
//    public bool interactable = true;
//    public bool enableShadowEffect = true;
//    //represent the index of the sprite shown in the right page
//    public int currentPage = 0;
//    public int TotalPageCount
//    {
//        get { return bookPages.Length; }
//    }
//    public Vector3 EndBottomLeft
//    {
//        get { return ebl; }
//    }
//    public Vector3 EndBottomRight
//    {
//        get { return ebr; }
//    }
//    public float Height
//    {
//        get
//        {
//            return BookPanel.rect.height;
//        }
//    }
//    public Image ClippingPlane;
//    public Image NextPageClip;
//    public Image Shadow;
//    public Image ShadowLTR;
//    public Image Left;
//    public Image LeftNext;
//    public Image Right;
//    public Image RightNext;
//    public UnityEvent OnFlip;
//    float radius1, radius2;
//    //Spine Bottom
//    Vector3 sb;
//    //Spine Top
//    Vector3 st;
//    //corner of the page
//    Vector3 c;
//    //Edge Bottom Right
//    Vector3 ebr;
//    //Edge Bottom Left
//    Vector3 ebl;
//    //follow point 
//    Vector3 f;
//    bool pageDragging = false;
//    //current flip mode
//    FlipMode mode;

//    void Start()
//    {
//        if (!canvas) canvas = GetComponentInParent<Canvas>();
//        if (!canvas) Debug.LogError("Book should be a child to canvas");

//        Left.gameObject.SetActive(false);
//        Right.gameObject.SetActive(false);
//        UpdateSprites();
//        CalcCurlCriticalPoints();

//        float pageWidth = BookPanel.rect.width / 2.0f;
//        float pageHeight = BookPanel.rect.height;
//        NextPageClip.rectTransform.sizeDelta = new Vector2(pageWidth, pageHeight + pageHeight * 2);


//        ClippingPlane.rectTransform.sizeDelta = new Vector2(pageWidth * 2 + pageHeight, pageHeight + pageHeight * 2);

//        //hypotenous (diagonal) page length
//        float hyp = Mathf.Sqrt(pageWidth * pageWidth + pageHeight * pageHeight);
//        float shadowPageHeight = pageWidth / 2 + hyp;

//        Shadow.rectTransform.sizeDelta = new Vector2(pageWidth, shadowPageHeight);
//        Shadow.rectTransform.pivot = new Vector2(1, (pageWidth / 2) / shadowPageHeight);

//        ShadowLTR.rectTransform.sizeDelta = new Vector2(pageWidth, shadowPageHeight);
//        ShadowLTR.rectTransform.pivot = new Vector2(0, (pageWidth / 2) / shadowPageHeight);

//    }

//    private void CalcCurlCriticalPoints()
//    {
//        sb = new Vector3(0, -BookPanel.rect.height / 2);
//        ebr = new Vector3(BookPanel.rect.width / 2, -BookPanel.rect.height / 2);
//        ebl = new Vector3(-BookPanel.rect.width / 2, -BookPanel.rect.height / 2);
//        st = new Vector3(0, BookPanel.rect.height / 2);
//        radius1 = Vector2.Distance(sb, ebr);
//        float pageWidth = BookPanel.rect.width / 2.0f;
//        float pageHeight = BookPanel.rect.height;
//        radius2 = Mathf.Sqrt(pageWidth * pageWidth + pageHeight * pageHeight);
//    }

//    //public Vector3 transformPoint(Vector3 mouseScreenPos)
//    //{
//    //    if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
//    //    {
//    //        Vector3 mouseWorldPos = canvas.worldCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, canvas.planeDistance));
//    //        Vector2 localPos = BookPanel.InverseTransformPoint(mouseWorldPos);

//    //        return localPos;
//    //    }
//    //    else if (canvas.renderMode == RenderMode.WorldSpace)
//    //    {
//    //        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
//    //        Vector3 globalEBR = transform.TransformPoint(ebr);
//    //        Vector3 globalEBL = transform.TransformPoint(ebl);
//    //        Vector3 globalSt = transform.TransformPoint(st);
//    //        Plane p = new Plane(globalEBR, globalEBL, globalSt);
//    //        float distance;
//    //        p.Raycast(ray, out distance);
//    //        Vector2 localPos = BookPanel.InverseTransformPoint(ray.GetPoint(distance));
//    //        return localPos;
//    //    }
//    //    else
//    //    {
//    //        //Screen Space Overlay
//    //        Vector2 localPos = BookPanel.InverseTransformPoint(mouseScreenPos);
//    //        return localPos;
//    //    }
//    //}

//    public Vector3 transformPoint(Vector3 mouseScreenPos)
//    {
//        if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
//        {
//            Vector3 mouseWorldPos = canvas.worldCamera.ScreenToWorldPoint(
//                new Vector3(mouseScreenPos.x, mouseScreenPos.y, canvas.planeDistance));
//            return BookPanel.InverseTransformPoint(mouseWorldPos);
//        }
//        else if (canvas.renderMode == RenderMode.WorldSpace)
//        {
//            // ✅ Cast a ray from camera to touch/mouse point
//            Ray ray = canvas.worldCamera.ScreenPointToRay(mouseScreenPos);

//            // ✅ Define a virtual plane based on BookPanel facing direction
//            Plane bookPlane = new Plane(BookPanel.forward, BookPanel.position);

//            if (bookPlane.Raycast(ray, out float distance))
//            {
//                Vector3 hitPoint = ray.GetPoint(distance);
//                return BookPanel.InverseTransformPoint(hitPoint);
//            }
//            else
//            {
//                Debug.LogWarning("[Book] ⚠️ Raycast failed to hit book panel in World Space.");
//                return Vector3.zero;
//            }
//        }
//        else
//        {
//            // Fallback for ScreenSpace Overlay
//            return BookPanel.InverseTransformPoint(mouseScreenPos);
//        }
//    }

//    void Update()
//    {
//        if (pageDragging && interactable)
//        {
//            UpdateBook();
//        }
//    }
//    public void UpdateBook()
//    {
//        f = Vector3.Lerp(f, transformPoint(Input.mousePosition), Time.deltaTime * 10);
//        if (mode == FlipMode.RightToLeft)
//            UpdateBookRTLToPoint(f);
//        else
//            UpdateBookLTRToPoint(f);
//    }
//    public void UpdateBookLTRToPoint(Vector3 followLocation)
//    {
//        mode = FlipMode.LeftToRight;
//        f = followLocation;
//        ShadowLTR.transform.SetParent(ClippingPlane.transform, true);
//        ShadowLTR.transform.localPosition = new Vector3(0, 0, 0);
//        ShadowLTR.transform.localEulerAngles = new Vector3(0, 0, 0);
//        Left.transform.SetParent(ClippingPlane.transform, true);

//        Right.transform.SetParent(BookPanel.transform, true);
//        Right.transform.localEulerAngles = Vector3.zero;
//        LeftNext.transform.SetParent(BookPanel.transform, true);

//        c = Calc_C_Position(followLocation);
//        Vector3 t1;
//        float clipAngle = CalcClipAngle(c, ebl, out t1);
//        //0 < T0_T1_Angle < 180
//        clipAngle = (clipAngle + 180) % 180;

//        ClippingPlane.transform.localEulerAngles = new Vector3(0, 0, clipAngle - 90);
//        ClippingPlane.transform.position = BookPanel.TransformPoint(t1);

//        //page position and angle
//        Left.transform.position = BookPanel.TransformPoint(c);
//        float C_T1_dy = t1.y - c.y;
//        float C_T1_dx = t1.x - c.x;
//        float C_T1_Angle = Mathf.Atan2(C_T1_dy, C_T1_dx) * Mathf.Rad2Deg;
//        Left.transform.localEulerAngles = new Vector3(0, 0, C_T1_Angle - 90 - clipAngle);

//        NextPageClip.transform.localEulerAngles = new Vector3(0, 0, clipAngle - 90);
//        NextPageClip.transform.position = BookPanel.TransformPoint(t1);
//        LeftNext.transform.SetParent(NextPageClip.transform, true);
//        Right.transform.SetParent(ClippingPlane.transform, true);
//        Right.transform.SetAsFirstSibling();

//        ShadowLTR.rectTransform.SetParent(Left.rectTransform, true);
//    }
//    public void UpdateBookRTLToPoint(Vector3 followLocation)
//    {
//        mode = FlipMode.RightToLeft;
//        f = followLocation;
//        Shadow.transform.SetParent(ClippingPlane.transform, true);
//        Shadow.transform.localPosition = Vector3.zero;
//        Shadow.transform.localEulerAngles = Vector3.zero;
//        Right.transform.SetParent(ClippingPlane.transform, true);

//        Left.transform.SetParent(BookPanel.transform, true);
//        Left.transform.localEulerAngles = Vector3.zero;
//        RightNext.transform.SetParent(BookPanel.transform, true);
//        c = Calc_C_Position(followLocation);
//        Vector3 t1;
//        float clipAngle = CalcClipAngle(c, ebr, out t1);
//        if (clipAngle > -90) clipAngle += 180;

//        ClippingPlane.rectTransform.pivot = new Vector2(1, 0.35f);
//        ClippingPlane.transform.localEulerAngles = new Vector3(0, 0, clipAngle + 90);
//        ClippingPlane.transform.position = BookPanel.TransformPoint(t1);

//        //page position and angle
//        Right.transform.position = BookPanel.TransformPoint(c);
//        float C_T1_dy = t1.y - c.y;
//        float C_T1_dx = t1.x - c.x;
//        float C_T1_Angle = Mathf.Atan2(C_T1_dy, C_T1_dx) * Mathf.Rad2Deg;
//        Right.transform.localEulerAngles = new Vector3(0, 0, C_T1_Angle - (clipAngle + 90));

//        NextPageClip.transform.localEulerAngles = new Vector3(0, 0, clipAngle + 90);
//        NextPageClip.transform.position = BookPanel.TransformPoint(t1);
//        RightNext.transform.SetParent(NextPageClip.transform, true);
//        Left.transform.SetParent(ClippingPlane.transform, true);
//        Left.transform.SetAsFirstSibling();

//        Shadow.rectTransform.SetParent(Right.rectTransform, true);
//    }
//    private float CalcClipAngle(Vector3 c, Vector3 bookCorner, out Vector3 t1)
//    {
//        Vector3 t0 = (c + bookCorner) / 2;
//        float T0_CORNER_dy = bookCorner.y - t0.y;
//        float T0_CORNER_dx = bookCorner.x - t0.x;
//        float T0_CORNER_Angle = Mathf.Atan2(T0_CORNER_dy, T0_CORNER_dx);
//        float T0_T1_Angle = 90 - T0_CORNER_Angle;

//        float T1_X = t0.x - T0_CORNER_dy * Mathf.Tan(T0_CORNER_Angle);
//        T1_X = normalizeT1X(T1_X, bookCorner, sb);
//        t1 = new Vector3(T1_X, sb.y, 0);

//        //clipping plane angle=T0_T1_Angle
//        float T0_T1_dy = t1.y - t0.y;
//        float T0_T1_dx = t1.x - t0.x;
//        T0_T1_Angle = Mathf.Atan2(T0_T1_dy, T0_T1_dx) * Mathf.Rad2Deg;
//        return T0_T1_Angle;
//    }
//    private float normalizeT1X(float t1, Vector3 corner, Vector3 sb)
//    {
//        if (t1 > sb.x && sb.x > corner.x)
//            return sb.x;
//        if (t1 < sb.x && sb.x < corner.x)
//            return sb.x;
//        return t1;
//    }
//    private Vector3 Calc_C_Position(Vector3 followLocation)
//    {
//        Vector3 c;
//        f = followLocation;
//        float F_SB_dy = f.y - sb.y;
//        float F_SB_dx = f.x - sb.x;
//        float F_SB_Angle = Mathf.Atan2(F_SB_dy, F_SB_dx);
//        Vector3 r1 = new Vector3(radius1 * Mathf.Cos(F_SB_Angle), radius1 * Mathf.Sin(F_SB_Angle), 0) + sb;

//        float F_SB_distance = Vector2.Distance(f, sb);
//        if (F_SB_distance < radius1)
//            c = f;
//        else
//            c = r1;
//        float F_ST_dy = c.y - st.y;
//        float F_ST_dx = c.x - st.x;
//        float F_ST_Angle = Mathf.Atan2(F_ST_dy, F_ST_dx);
//        Vector3 r2 = new Vector3(radius2 * Mathf.Cos(F_ST_Angle),
//           radius2 * Mathf.Sin(F_ST_Angle), 0) + st;
//        float C_ST_distance = Vector2.Distance(c, st);
//        if (C_ST_distance > radius2)
//            c = r2;
//        return c;
//    }
//    public void DragRightPageToPoint(Vector3 point)
//    {
//        if (currentPage >= bookPages.Length) return;
//        pageDragging = true;
//        mode = FlipMode.RightToLeft;
//        f = point;


//        NextPageClip.rectTransform.pivot = new Vector2(0, 0.12f);
//        ClippingPlane.rectTransform.pivot = new Vector2(1, 0.35f);

//        Left.gameObject.SetActive(true);
//        Left.rectTransform.pivot = new Vector2(0, 0);
//        Left.transform.position = RightNext.transform.position;
//        Left.transform.eulerAngles = new Vector3(0, 0, 0);
//        Left.sprite = (currentPage < bookPages.Length) ? bookPages[currentPage] : background;
//        Left.transform.SetAsFirstSibling();

//        Right.gameObject.SetActive(true);
//        Right.transform.position = RightNext.transform.position;
//        Right.transform.eulerAngles = new Vector3(0, 0, 0);
//        Right.sprite = (currentPage < bookPages.Length - 1) ? bookPages[currentPage + 1] : background;

//        RightNext.sprite = (currentPage < bookPages.Length - 2) ? bookPages[currentPage + 2] : background;

//        LeftNext.transform.SetAsFirstSibling();
//        if (enableShadowEffect) Shadow.gameObject.SetActive(true);
//        UpdateBookRTLToPoint(f);
//    }
//    public void OnMouseDragRightPage()
//    {
//        if (interactable)
//            DragRightPageToPoint(transformPoint(Input.mousePosition));

//    }
//    public void DragLeftPageToPoint(Vector3 point)
//    {
//        if (currentPage <= 0) return;
//        pageDragging = true;
//        mode = FlipMode.LeftToRight;
//        f = point;

//        NextPageClip.rectTransform.pivot = new Vector2(1, 0.12f);
//        ClippingPlane.rectTransform.pivot = new Vector2(0, 0.35f);

//        Right.gameObject.SetActive(true);
//        Right.transform.position = LeftNext.transform.position;
//        Right.sprite = bookPages[currentPage - 1];
//        Right.transform.eulerAngles = new Vector3(0, 0, 0);
//        Right.transform.SetAsFirstSibling();

//        Left.gameObject.SetActive(true);
//        Left.rectTransform.pivot = new Vector2(1, 0);
//        Left.transform.position = LeftNext.transform.position;
//        Left.transform.eulerAngles = new Vector3(0, 0, 0);
//        Left.sprite = (currentPage >= 2) ? bookPages[currentPage - 2] : background;

//        LeftNext.sprite = (currentPage >= 3) ? bookPages[currentPage - 3] : background;

//        RightNext.transform.SetAsFirstSibling();
//        if (enableShadowEffect) ShadowLTR.gameObject.SetActive(true);
//        UpdateBookLTRToPoint(f);
//    }
//    public void OnMouseDragLeftPage()
//    {
//        if (interactable)
//            DragLeftPageToPoint(transformPoint(Input.mousePosition));

//    }
//    public void OnMouseRelease()
//    {
//        if (interactable)
//            ReleasePage();
//    }
//    public void ReleasePage()
//    {
//        if (pageDragging)
//        {
//            pageDragging = false;
//            float distanceToLeft = Vector2.Distance(c, ebl);
//            float distanceToRight = Vector2.Distance(c, ebr);
//            if (distanceToRight < distanceToLeft && mode == FlipMode.RightToLeft)
//                TweenBack();
//            else if (distanceToRight > distanceToLeft && mode == FlipMode.LeftToRight)
//                TweenBack();
//            else
//                TweenForward();
//        }
//    }
//    Coroutine currentCoroutine;
//    void UpdateSprites()
//    {
//        LeftNext.sprite = (currentPage > 0 && currentPage <= bookPages.Length) ? bookPages[currentPage - 1] : background;
//        RightNext.sprite = (currentPage >= 0 && currentPage < bookPages.Length) ? bookPages[currentPage] : background;
//    }
//    public void TweenForward()
//    {
//        if (mode == FlipMode.RightToLeft)
//            currentCoroutine = StartCoroutine(TweenTo(ebl, 0.15f, () => { Flip(); }));
//        else
//            currentCoroutine = StartCoroutine(TweenTo(ebr, 0.15f, () => { Flip(); }));
//    }
//    void Flip()
//    {
//        if (mode == FlipMode.RightToLeft)
//            currentPage += 2;
//        else
//            currentPage -= 2;
//        LeftNext.transform.SetParent(BookPanel.transform, true);
//        Left.transform.SetParent(BookPanel.transform, true);
//        LeftNext.transform.SetParent(BookPanel.transform, true);
//        Left.gameObject.SetActive(false);
//        Right.gameObject.SetActive(false);
//        Right.transform.SetParent(BookPanel.transform, true);
//        RightNext.transform.SetParent(BookPanel.transform, true);
//        UpdateSprites();
//        Shadow.gameObject.SetActive(false);
//        ShadowLTR.gameObject.SetActive(false);
//        if (OnFlip != null)
//            OnFlip.Invoke();
//    }
//    public void TweenBack()
//    {
//        if (mode == FlipMode.RightToLeft)
//        {
//            currentCoroutine = StartCoroutine(TweenTo(ebr, 0.15f,
//                () =>
//                {
//                    UpdateSprites();
//                    RightNext.transform.SetParent(BookPanel.transform);
//                    Right.transform.SetParent(BookPanel.transform);

//                    Left.gameObject.SetActive(false);
//                    Right.gameObject.SetActive(false);
//                    pageDragging = false;
//                }
//                ));
//        }
//        else
//        {
//            currentCoroutine = StartCoroutine(TweenTo(ebl, 0.15f,
//                () =>
//                {
//                    UpdateSprites();

//                    LeftNext.transform.SetParent(BookPanel.transform);
//                    Left.transform.SetParent(BookPanel.transform);

//                    Left.gameObject.SetActive(false);
//                    Right.gameObject.SetActive(false);
//                    pageDragging = false;
//                }
//                ));
//        }
//    }
//    public IEnumerator TweenTo(Vector3 to, float duration, System.Action onFinish)
//    {
//        int steps = (int)(duration / 0.025f);
//        Vector3 displacement = (to - f) / steps;
//        for (int i = 0; i < steps - 1; i++)
//        {
//            if (mode == FlipMode.RightToLeft)
//                UpdateBookRTLToPoint(f + displacement);
//            else
//                UpdateBookLTRToPoint(f + displacement);

//            yield return new WaitForSeconds(0.025f);
//        }
//        if (onFinish != null)
//            onFinish();
//    }
//}

//using UnityEngine;
//using System.Collections;
//using UnityEngine.UI;
//using UnityEngine.Events;

//public enum FlipMode
//{
//    RightToLeft,
//    LeftToRight
//}

//[ExecuteInEditMode]
//public class Book : MonoBehaviour
//{
//    public Canvas canvas;
//    [SerializeField] RectTransform BookPanel;
//    public GameObject[] stepPanels; // UI prefabs for each step (half-page panels)
//    public bool interactable = true;
//    public bool enableShadowEffect = true;
//    public int currentPage = 0;

//    public Image ClippingPlane;
//    public Image NextPageClip;
//    public Image Shadow;
//    public Image ShadowLTR;
//    public Image Left;
//    public Image LeftNext;
//    public Image Right;
//    public Image RightNext;
//    public UnityEvent OnFlip;

//    private float radius1, radius2;
//    private Vector3 sb, st, c, ebr, ebl, f;
//    private bool pageDragging = false;
//    private FlipMode mode;

//    void Start()
//    {
//        if (!canvas) canvas = GetComponentInParent<Canvas>();
//        if (!canvas) Debug.LogError("Book should be a child to canvas");

//        Left.gameObject.SetActive(false);
//        Right.gameObject.SetActive(false);
//        UpdateStepPanels();
//        CalcCurlCriticalPoints();
//        SetupShadows();
//    }

//    void CalcCurlCriticalPoints()
//    {
//        sb = new Vector3(0, -BookPanel.rect.height / 2);
//        ebr = new Vector3(BookPanel.rect.width / 2, -BookPanel.rect.height / 2);
//        ebl = new Vector3(-BookPanel.rect.width / 2, -BookPanel.rect.height / 2);
//        st = new Vector3(0, BookPanel.rect.height / 2);
//        float pageWidth = BookPanel.rect.width / 2.0f;
//        float pageHeight = BookPanel.rect.height;
//        radius1 = Vector2.Distance(sb, ebr);
//        radius2 = Mathf.Sqrt(pageWidth * pageWidth + pageHeight * pageHeight);
//    }

//    void SetupShadows()
//    {
//        float pageWidth = BookPanel.rect.width / 2.0f;
//        float pageHeight = BookPanel.rect.height;
//        float hyp = Mathf.Sqrt(pageWidth * pageWidth + pageHeight * pageHeight);
//        float shadowPageHeight = pageWidth / 2 + hyp;

//        NextPageClip.rectTransform.sizeDelta = new Vector2(pageWidth, pageHeight * 3);
//        ClippingPlane.rectTransform.sizeDelta = new Vector2(pageWidth * 2 + pageHeight, pageHeight * 3);

//        Shadow.rectTransform.sizeDelta = new Vector2(pageWidth, shadowPageHeight);
//        Shadow.rectTransform.pivot = new Vector2(1, (pageWidth / 2) / shadowPageHeight);

//        ShadowLTR.rectTransform.sizeDelta = new Vector2(pageWidth, shadowPageHeight);
//        ShadowLTR.rectTransform.pivot = new Vector2(0, (pageWidth / 2) / shadowPageHeight);
//    }

//    void Update()
//    {
//        if (!interactable || Input.touchCount == 0) return;

//        if (Input.GetTouch(0).phase == TouchPhase.Began)
//        {
//            Vector3 touchPos = Input.GetTouch(0).position;
//            Vector3 localTouch = transformPoint(touchPos);

//            if (localTouch.x > 0)
//            {
//                ShowNextPage();  // 👉 Right side: next
//            }
//            else
//            {
//                ShowPreviousPage();  // 👈 Left side: previous
//            }
//        }
//    }

//    void ShowNextPage()
//    {
//        if (currentPage + 1 >= stepPanels.Length) return;

//        currentPage += 1;
//        UpdateStepPanels();
//    }

//    void ShowPreviousPage()
//    {
//        if (currentPage - 1 < 0) return;

//        currentPage -= 1;
//        UpdateStepPanels();
//    }



//    public void OnMouseDragRightPage()
//    {
//        if (interactable)
//            DragRightPageToPoint(transformPoint(Input.mousePosition));
//    }

//    public void OnMouseDragLeftPage()
//    {
//        if (interactable)
//            DragLeftPageToPoint(transformPoint(Input.mousePosition));
//    }

//    public void OnMouseRelease()
//    {
//        if (interactable)
//            ReleasePage();
//    }

//    void UpdateBook()
//    {
//        f = Vector3.Lerp(f, transformPoint(Input.mousePosition), Time.deltaTime * 10);
//        if (mode == FlipMode.RightToLeft)
//            UpdateBookRTLToPoint(f);
//        else
//            UpdateBookLTRToPoint(f);
//    }

//    public void DragRightPageToPoint(Vector3 point)
//    {
//        if (currentPage >= stepPanels.Length) return;
//        pageDragging = true;
//        mode = FlipMode.RightToLeft;
//        f = point;

//        PrepareNextPanels();
//        UpdateBookRTLToPoint(f);
//    }

//    public void DragLeftPageToPoint(Vector3 point)
//    {
//        if (currentPage <= 0) return;
//        pageDragging = true;
//        mode = FlipMode.LeftToRight;
//        f = point;

//        PreparePreviousPanels();
//        UpdateBookLTRToPoint(f);
//    }

//    void PrepareNextPanels()
//    {
//        ClearPanel(LeftNext);
//        ClearPanel(RightNext);

//        if (currentPage < stepPanels.Length)
//            Instantiate(stepPanels[currentPage], RightNext.transform);
//        if (currentPage + 1 < stepPanels.Length)
//            Instantiate(stepPanels[currentPage + 1], LeftNext.transform);

//        Left.transform.position = RightNext.transform.position;
//        Right.transform.position = RightNext.transform.position;
//        Left.transform.eulerAngles = Vector3.zero;
//        Right.transform.eulerAngles = Vector3.zero;

//        Left.gameObject.SetActive(true);
//        Right.gameObject.SetActive(true);
//        LeftNext.transform.SetAsFirstSibling();

//        if (enableShadowEffect) Shadow.gameObject.SetActive(true);
//    }

//    void PreparePreviousPanels()
//    {
//        ClearPanel(LeftNext);
//        ClearPanel(RightNext);

//        if (currentPage - 2 >= 0)
//            Instantiate(stepPanels[currentPage - 2], LeftNext.transform);
//        if (currentPage - 1 >= 0)
//            Instantiate(stepPanels[currentPage - 1], RightNext.transform);

//        Left.transform.position = LeftNext.transform.position;
//        Right.transform.position = LeftNext.transform.position;
//        Left.transform.eulerAngles = Vector3.zero;
//        Right.transform.eulerAngles = Vector3.zero;

//        Left.gameObject.SetActive(true);
//        Right.gameObject.SetActive(true);
//        RightNext.transform.SetAsFirstSibling();

//        if (enableShadowEffect) ShadowLTR.gameObject.SetActive(true);
//    }

//    private void ClearPanel(Image target)
//    {
//        foreach (Transform child in target.transform)
//        {
//#if UNITY_EDITOR
//            if (!Application.isPlaying)
//                DestroyImmediate(child.gameObject);
//            else
//                Destroy(child.gameObject);
//#else
//        Destroy(child.gameObject);
//#endif
//        }
//    }


//    void UpdateStepPanels()
//    {
//        ClearPanel(LeftNext);
//        ClearPanel(RightNext);

//        if (currentPage >= 0 && currentPage < stepPanels.Length)
//            Instantiate(stepPanels[currentPage], RightNext.transform);
//    }


//    void Flip()
//    {
//        if (mode == FlipMode.RightToLeft)
//            currentPage += 2;
//        else
//            currentPage -= 2;

//        // Reset basic layout
//        LeftNext.transform.SetParent(BookPanel.transform, true);
//        Left.transform.SetParent(BookPanel.transform, true);
//        Right.transform.SetParent(BookPanel.transform, true);
//        RightNext.transform.SetParent(BookPanel.transform, true);

//        Left.gameObject.SetActive(false);
//        Right.gameObject.SetActive(false);
//        Shadow.gameObject.SetActive(false);
//        ShadowLTR.gameObject.SetActive(false);

//        // Clear previous panel content
//        ClearPanel(LeftNext);
//        ClearPanel(RightNext);

//        // Add next step panels
//        if (currentPage < stepPanels.Length)
//        {
//            GameObject leftPanel = Instantiate(stepPanels[currentPage], LeftNext.transform);
//            leftPanel.transform.localPosition = Vector3.zero;
//            leftPanel.transform.localRotation = Quaternion.identity;
//            leftPanel.transform.localScale = Vector3.one;
//        }

//        if (currentPage + 1 < stepPanels.Length)
//        {
//            GameObject rightPanel = Instantiate(stepPanels[currentPage + 1], RightNext.transform);
//            rightPanel.transform.localPosition = Vector3.zero;
//            rightPanel.transform.localRotation = Quaternion.identity;
//            rightPanel.transform.localScale = Vector3.one;
//        }

//        if (OnFlip != null)
//            OnFlip.Invoke();
//    }


//    void ResetPanels()
//    {
//        LeftNext.transform.SetParent(BookPanel.transform, true);
//        Left.transform.SetParent(BookPanel.transform, true);
//        Right.transform.SetParent(BookPanel.transform, true);
//        RightNext.transform.SetParent(BookPanel.transform, true);

//        Left.gameObject.SetActive(false);
//        Right.gameObject.SetActive(false);
//    }

//    public void TweenForward()
//    {
//        Vector3 target = (mode == FlipMode.RightToLeft) ? ebl : ebr;
//        StartCoroutine(TweenTo(target, 0.15f, Flip));
//    }

//    public void TweenBack()
//    {
//        Vector3 target = (mode == FlipMode.RightToLeft) ? ebr : ebl;
//        StartCoroutine(TweenTo(target, 0.15f, ResetPanels));
//    }

//    public IEnumerator TweenTo(Vector3 to, float duration, System.Action onFinish)
//    {
//        int steps = (int)(duration / 0.025f);
//        Vector3 displacement = (to - f) / steps;
//        for (int i = 0; i < steps - 1; i++)
//        {
//            f += displacement;
//            if (mode == FlipMode.RightToLeft)
//                UpdateBookRTLToPoint(f);
//            else
//                UpdateBookLTRToPoint(f);
//            yield return new WaitForSeconds(0.025f);
//        }
//        onFinish?.Invoke();
//    }

//    public void ReleasePage()
//    {
//        if (!pageDragging) return;
//        pageDragging = false;
//        float distToLeft = Vector2.Distance(c, ebl);
//        float distToRight = Vector2.Distance(c, ebr);
//        if ((mode == FlipMode.RightToLeft && distToRight < distToLeft) ||
//            (mode == FlipMode.LeftToRight && distToRight > distToLeft))
//            TweenBack();
//        else
//            TweenForward();
//    }

//    public Vector3 transformPoint(Vector3 mouseScreenPos)
//    {
//        if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
//        {
//            Vector3 world = canvas.worldCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, canvas.planeDistance));
//            return BookPanel.InverseTransformPoint(world);
//        }
//        else if (canvas.renderMode == RenderMode.WorldSpace)
//        {
//            Ray ray = canvas.worldCamera.ScreenPointToRay(mouseScreenPos);
//            Plane plane = new Plane(BookPanel.forward, BookPanel.position);
//            if (plane.Raycast(ray, out float dist))
//                return BookPanel.InverseTransformPoint(ray.GetPoint(dist));
//            return Vector3.zero;
//        }
//        else
//        {
//            return BookPanel.InverseTransformPoint(mouseScreenPos);
//        }
//    }

//    float CalcClipAngle(Vector3 c, Vector3 corner, out Vector3 t1)
//    {
//        Vector3 t0 = (c + corner) / 2;
//        float dy = corner.y - t0.y;
//        float dx = corner.x - t0.x;
//        float angle = Mathf.Atan2(dy, dx);
//        float t1x = t0.x - dy * Mathf.Tan(angle);
//        t1x = NormalizeT1X(t1x, corner, sb);
//        t1 = new Vector3(t1x, sb.y, 0);
//        float ddy = t1.y - t0.y;
//        float ddx = t1.x - t0.x;
//        return Mathf.Atan2(ddy, ddx) * Mathf.Rad2Deg;
//    }

//    float NormalizeT1X(float t1, Vector3 corner, Vector3 sb)
//    {
//        if ((t1 > sb.x && sb.x > corner.x) || (t1 < sb.x && sb.x < corner.x))
//            return sb.x;
//        return t1;
//    }

//    Vector3 Calc_C_Position(Vector3 followLocation)
//    {
//        f = followLocation;
//        Vector3 r1 = sb + radius1 * (f - sb).normalized;
//        Vector3 c = Vector2.Distance(f, sb) < radius1 ? f : r1;
//        Vector3 r2 = st + radius2 * (c - st).normalized;
//        return Vector2.Distance(c, st) > radius2 ? r2 : c;
//    }

//    void UpdateBookRTLToPoint(Vector3 follow)
//    {
//        c = Calc_C_Position(follow);
//        Vector3 t1;
//        float angle = CalcClipAngle(c, ebr, out t1);
//        if (angle > -90) angle += 180;

//        ClippingPlane.rectTransform.pivot = new Vector2(1, 0.35f);
//        ClippingPlane.transform.localEulerAngles = new Vector3(0, 0, angle + 90);
//        ClippingPlane.transform.position = BookPanel.TransformPoint(t1);

//        Right.transform.position = BookPanel.TransformPoint(c);
//        float dy = t1.y - c.y, dx = t1.x - c.x;
//        Right.transform.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(dy, dx) * Mathf.Rad2Deg - (angle + 90));

//        NextPageClip.transform.localEulerAngles = new Vector3(0, 0, angle + 90);
//        NextPageClip.transform.position = BookPanel.TransformPoint(t1);
//        RightNext.transform.SetParent(NextPageClip.transform, true);
//        Left.transform.SetParent(ClippingPlane.transform, true);
//        Left.transform.SetAsFirstSibling();
//        Shadow.rectTransform.SetParent(Right.rectTransform, true);
//    }

//    void UpdateBookLTRToPoint(Vector3 follow)
//    {
//        c = Calc_C_Position(follow);
//        Vector3 t1;
//        float angle = CalcClipAngle(c, ebl, out t1);
//        angle = (angle + 180) % 180;

//        ClippingPlane.transform.localEulerAngles = new Vector3(0, 0, angle - 90);
//        ClippingPlane.transform.position = BookPanel.TransformPoint(t1);

//        Left.transform.position = BookPanel.TransformPoint(c);
//        float dy = t1.y - c.y, dx = t1.x - c.x;
//        Left.transform.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(dy, dx) * Mathf.Rad2Deg - 90 - angle);

//        NextPageClip.transform.localEulerAngles = new Vector3(0, 0, angle - 90);
//        NextPageClip.transform.position = BookPanel.TransformPoint(t1);
//        LeftNext.transform.SetParent(NextPageClip.transform, true);
//        Right.transform.SetParent(ClippingPlane.transform, true);
//        Right.transform.SetAsFirstSibling();
//        ShadowLTR.rectTransform.SetParent(Left.rectTransform, true);
//    }
//}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class Book : MonoBehaviour
{
    public Canvas canvas;
    [SerializeField] RectTransform BookPanel;
    public GameObject[] stepPanels; // Prefabs for each page
    public bool interactable = true;

    [Header("Fixed Content Panels")]
    public Transform leftPanel;
    public Transform rightPanel;

    [Header("Tap Regions")]
    public RectTransform prevRegion;
    public RectTransform nextRegion;


    public Image ClippingPlane;
    public Image NextPageClip;
    public Image Shadow;
    public Image ShadowLTR;
    public Image Left;
    public Image LeftNext;
    public Image Right;
    public Image RightNext;
    public UnityEvent OnFlip;

    public int currentPage = 0;

    void Start()
    {
        if (!canvas) canvas = GetComponentInParent<Canvas>();
        if (!canvas) Debug.LogError("Book should be a child to canvas");

        Left.gameObject.SetActive(false);
        Right.gameObject.SetActive(false);

        Shadow.gameObject.SetActive(false);
        ShadowLTR.gameObject.SetActive(false);

        currentPage = 0;
        UpdateStepPanels();
    }

    void Update()
    {
        if (!interactable || Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            Vector2 screenPos = touch.position;
            Debug.Log($"📱 Touch position: {screenPos}");

            if (nextRegion != null && RectTransformUtility.RectangleContainsScreenPoint(nextRegion, screenPos, canvas.worldCamera))
            {
                Debug.Log("✅ NEXT region tapped");
                ShowNextPagePair();
            }
            else if (prevRegion != null && RectTransformUtility.RectangleContainsScreenPoint(prevRegion, screenPos, canvas.worldCamera))
            {
                Debug.Log("✅ PREVIOUS region tapped");
                ShowPreviousPagePair();
            }
            else
            {
                Debug.Log("❌ Tap missed both regions");
            }
        }
    }



    void ShowNextPagePair()
    {
        if (currentPage + 2 >= stepPanels.Length) return;
        currentPage += 2;
        UpdateStepPanels();
    }

    void ShowPreviousPagePair()
    {
        if (currentPage - 2 < 0) return;
        currentPage -= 2;
        UpdateStepPanels();
    }

    void UpdateStepPanels()
    {
        ClearChildren(leftPanel);
        ClearChildren(rightPanel);

        if (currentPage >= 0 && currentPage < stepPanels.Length)
        {
            GameObject leftPage = Instantiate(stepPanels[currentPage], leftPanel);
            ResetRectTransform(leftPage);
        }

        if (currentPage + 1 < stepPanels.Length)
        {
            GameObject rightPage = Instantiate(stepPanels[currentPage + 1], rightPanel);
            ResetRectTransform(rightPage);
        }

        Shadow.gameObject.SetActive(false);
        ShadowLTR.gameObject.SetActive(false);
    }

    void ClearChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(child.gameObject);
            else
                Destroy(child.gameObject);
#else
            Destroy(child.gameObject);
#endif
        }
    }

    void ResetRectTransform(GameObject panel)
    {
        RectTransform rt = panel.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.localPosition = Vector3.zero;
        }
    }

    public Vector3 transformPoint(Vector3 screenPosition)
    {
        if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            Vector3 world = canvas.worldCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, canvas.planeDistance));
            return BookPanel.InverseTransformPoint(world);
        }
        else if (canvas.renderMode == RenderMode.WorldSpace)
        {
            Ray ray = canvas.worldCamera.ScreenPointToRay(screenPosition);
            Plane plane = new Plane(BookPanel.forward, BookPanel.position);
            if (plane.Raycast(ray, out float dist))
                return BookPanel.InverseTransformPoint(ray.GetPoint(dist));
            return Vector3.zero;
        }
        else
        {
            return BookPanel.InverseTransformPoint(screenPosition);
        }
    }
}
