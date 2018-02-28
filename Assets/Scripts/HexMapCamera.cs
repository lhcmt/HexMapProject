using UnityEngine;
using System.Collections;

public class HexMapCamera : MonoBehaviour {
    //自己的实力，打开菜单时，禁止摄像机移动
    static HexMapCamera instance;
    //旋转和缩放
    Transform swivel, stick;

    //缩放级别，0表示完全缩小，1表示完全放大；
    float zoom = 1f;
    float rotationAngle;
    //最小最大缩放
    public float stickMinZoom, stickMaxZoom;

    public float swivelMinZoom, swivelMaxZoom;
    //在最大最小缩放时候的摄像机移动速度
    public float moveSpeedMinZoom, moveSpeedMaxZoom;

    public float rotationSpeed;

    //用于摄像机移动的边界
    public HexGrid grid;

    void Awake()
    {
        instance = this;
        swivel = transform.GetChild(0);
        stick = swivel.GetChild(0);
    }
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
        if (zoomDelta != 0f)
        {
            AdjustZoom(zoomDelta);
        }
        //Input中没有"Rotation"，需要自己创建，在edit
        float rotationDelta = Input.GetAxis("Rotation");
        if(rotationDelta != 0f)
        {
            AdjustRotation(rotationDelta);
        }
        //方向键移动摄像机 值-1到1
        float xDelta = Input.GetAxis("Horizontal");
        float zDelta = Input.GetAxis("Vertical");
        if (xDelta != 0f || zDelta != 0f)
        {
            AdjustPosition(xDelta, zDelta);
        }
	}
    void AdjustRotation(float delta)
    {
        rotationAngle += delta * rotationSpeed * Time.deltaTime;
        if (rotationAngle < 0f)
        {
            rotationAngle += 360f;
        }
        else if (rotationAngle >= 360f)
        {
            rotationAngle -= 360f;
        }
        transform.localRotation = Quaternion.Euler(0f, rotationAngle, 0f);
    }
    void AdjustZoom(float delta)
    {
        //限制value在0,1之间并返回value。如果value小于0，返回0。如果value大于1,返回1，否则返回value 。
        zoom = Mathf.Clamp01(zoom + delta);
        //在最远和最近距离之间插值
        float distance = Mathf.Lerp(stickMinZoom, stickMaxZoom, zoom);
        stick.localPosition = new Vector3(0f, 0f, distance);

        float angle = Mathf.Lerp(swivelMinZoom, swivelMaxZoom, zoom);
        swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
    }

    void AdjustPosition(float xDelta, float zDelta)
    {
        Vector3 direction = transform.localRotation * new Vector3(xDelta, 0f, zDelta).normalized;
        //将最大的轴值作为运动的阻尼因子。
        float damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta));
        //每一秒的移动距离和帧数无关了
        float distance = Mathf.Lerp(moveSpeedMinZoom,moveSpeedMaxZoom,zoom)
            * damping * Time.deltaTime;

        Vector3 position = transform.localPosition;
        position += direction * distance;
        transform.localPosition = ClampPosition(position);

    }
    Vector3 ClampPosition(Vector3 position)
    {
        //x，z最大值,地图边界
        float xMax =
            (grid.cellCountX -0.5f) *
            (2f * HexMetrics.innerRadius);
        position.x = Mathf.Clamp(position.x, 0f, xMax);//限制value的值在min和max之间
        float zMax =
            (grid.cellCountZ - 1f) *
            (1.5f * HexMetrics.outerRadius);
        position.z = Mathf.Clamp(position.z, 0f, zMax);
        return position;
    }
    /*
     * true 摄像机无法移动
     * false 摄像机可以移动
     */
    public static bool Locked
    {
        set
        {
            instance.enabled = !value;
        }
    }
    /*
     * return to vaild position
     */
    public static void ValidatePosition()
    {
        instance.AdjustPosition(0f, 0f);
    }
}
