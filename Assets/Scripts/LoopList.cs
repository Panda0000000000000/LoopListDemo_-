/********************************************************************
	作者:   Panda
	日期:	2024/03/11
	author:		循环列表
	
	purpose:	
*********************************************************************/

using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System;

public class LoopList : MonoBehaviour, IEndDragHandler, IDragHandler, IBeginDragHandler
{
    public RectTransform item;
    public RectTransform content;

    /// <summary>
    /// 列表MoveToIndex时 移动的逻辑  分为顺序执行和距离最近  如果列表是单向，那就一定是顺序执行
    /// </summary>
    [SerializeField]
    private moveToIndexTypeEnum enum_MoveToIndexType = moveToIndexTypeEnum.shortDistance;
    private moveToIndexTypeEnum Enum_MoveToIndexType
    {
        get
        {
            if (Enum_LoopListType == loopListTypeEnum.singleDir)
            {
                return moveToIndexTypeEnum.orderLoop;
            }
            else
            {
                return enum_MoveToIndexType;
            }
        }
        set { enum_MoveToIndexType = value; }
    }
    
    /// <summary>
    /// 循环列表的类型   循环 单向
    /// </summary>
    [SerializeField]
    private loopListTypeEnum Enum_LoopListType= loopListTypeEnum.Loop;

    /// <summary>
    /// 单向移动时的阻尼距离 
    /// </summary>
    [Tooltip("单项列表时 到达边界时 列表可移动的最大长度")]
    public float float_MoveDampForSignleDir = 0f;

    /// <summary>
    /// 显示的数量 一定是奇数
    /// </summary>
    [Tooltip("显示的数量 可以被看到的Item的数量 Mask之内的，不会被遮住的部分")]
    public int int_ViewCount;

    /// <summary>
    /// 从哪个开始Index是1 
    /// </summary>
    [Tooltip("在列表初始化时 可以被看到的Item中，哪个是第一个 这里填下标，从0开始，")]
    public int int_ViewStartPos;

    /// <summary>
    /// 速度
    /// </summary>
    [Tooltip("列表滑动的速度 取值范围0-1  1就是最大的了")]
    public float float_MoveSpeed = 0.2f;

    /// <summary>
    /// 大小的缩放倍数
    /// </summary>
    [Tooltip("Item的缩放比例 列表中间的Item会被改变大小 就是根据这个的值来的")]
    public float float_Scaling;

    /// <summary>
    /// Y的缩放范围
    /// </summary>
    [Tooltip("Item的移动距离 列表中间的Item会被移动位置 就是根据这个的值来的")]
    public float float_YOffset;

    /// <summary>
    /// Item改变的参考线的偏移量
    /// </summary>
    [Tooltip("Item放大时  左右起始位置的偏移量")]
    public float float_ItemChangeLineOffset;

    [Tooltip("Item放大时  左边点位的倍数值 倍数是指Item宽度的倍数")]
    [SerializeField]
    float float_LeftChangePointValue = 1.5f;

    [Tooltip("Item放大时  右边点位的倍数值 倍数是指Item宽度的倍数")]
    [SerializeField]
    float float_RightChangePointValue = 3.5f;

    /// <summary>
    /// MoveToIndex时的移动曲线
    /// </summary>
    [Tooltip("Item移动时的速度曲线")]
    public AnimationCurve curve;

    /// <summary>
    /// 单个Item移动到目标位置需要的时间
    /// </summary>
    [Tooltip("单个Item的移动时间 从他的原始位置移动到一个位置时需要的时间")]
    public float float_SingleMoveItemTargetTime = 0.5f;

    /// <summary>
    /// 移动Item到目标位置 要经过多个Item时  每个Item的时间差量  要增加的时间
    /// </summary>
    [Tooltip("Item移动的递增时间 移动多个Item时 每个Item的递增时间值")]
    public float float_MoreItemMoveTargetTimeOffset = 0.05f;

    /// <summary>
    /// 可以移动的 X的差值 就是 滑动多少时 可以移动到下一个Item
    /// </summary>
    [Tooltip("移动列表时 可以移动到下一个Item时的移动距离 就是我在列表中移动多少时 就可以使列表切换到下一个Item了")]
    public int float_CanMoveItemOffsetX=70;

    //固定要移动到的Index的值 测试用  测完记得设置成0
    [Tooltip("测试用 MoveToIndex时的指定值 正式一定是0")]
    public int int_MoveIndex_Test = 0;

    /// <summary>
    /// item的创建数量
    /// </summary>
    private int creatCount;

    /// <summary>
    /// item的总数量
    /// </summary>
    private int _Count;

    private List<Item> itemList = new List<Item>();

    Action<GameObject,int,int> _callBackList;
    Action<GameObject,int,int>  _callBackMoveTargetEnd;

    int leftIndex;
    int rightIndex;
    int centerObjListIndex=0;
    float centerXPos;

    float left;
    float right;
    float center;

    float Epsilon = 0.001f;

    /// <summary>
    /// 是否输出列表相关的打印
    /// </summary>
    bool bool_IsDebug = false;


    public Component GetComponentType()
    {
        return this;
    }

    public void Init(int count, Action<GameObject,int,int> callBackList = null, Action<GameObject,int,int> moveTargetEnd = null)
    {
        _Count = count;
        _callBackList = callBackList;
        _callBackMoveTargetEnd = moveTargetEnd;

        CreatItem();
    }

    bool isCanEndDrag;
    bool isRefreshAllItem;
    private Vector3 dragPos;
    private Vector3 endDragPos;
    private Vector3 beginDragPos;

    public void OnBeginDrag(PointerEventData eventData)
    {
        isCanEndDrag = true;
        isRefreshAllItem = true;
        dragPos = eventData.position;
        beginDragPos = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {

        if (myCoroutine != null)
        {
            StopCoroutine(myCoroutine);
            isMoveing = false;
            myCoroutine = null;
        }

        float xMoveDis = dragPos.x - eventData.position.x;
        Log($"LoopList OnDrag xMoveDis:{xMoveDis}");
        MoveList(xMoveDis);
        dragPos = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        endDragPos = eventData.position;

        float distance = (endDragPos.x - beginDragPos.x) * float_MoveSpeed;

        if (!isCanEndDrag)
        {
            return;
        }
        int index = -1;
        int objIndex = -1;
        float minDis = centerXPos;
        for (int i = 0; i < creatCount; i++)
        {
            Item _item = itemList[i];
            float dis = Mathf.Abs(_item.rect.transform.localPosition.x - centerXPos);
            if (dis< minDis)
            {
                minDis = dis;
                index = _item.index;
                objIndex = i;
            }
        }

        if (objIndex == centerObjListIndex)
        {
            if (Mathf.Abs(distance) > float_CanMoveItemOffsetX && float_CanMoveItemOffsetX != 0)
            {
                objIndex = distance > 0 ? objIndex - 1 : objIndex + 1;
                index = 0;

            }
        }

        if (objIndex < 0)
        {
            objIndex = creatCount - 1;
        }

        if (objIndex >= creatCount)
        {
            objIndex = 0;
        }

        if (index != -1)
        {
            MoveToIndex(itemList[objIndex].index, objIndex);
        }

        isCanEndDrag = false;
    }

    /// <summary>
    /// 刷新列表 当时数量发生改变时执行这个
    /// </summary>
    /// <param name="count"></param>
    /// <param name="callBack">返回中间的obj 索引 objIndex</param>
    public void ManaRefresh(int count,int moveToIndex=-1,Action<GameObject,int,int> callBack=null)
    {
        Log("LoopList ManaRefresh");
        _Count = count;
        int centerIndex = moveToIndex!=-1?moveToIndex : itemList[centerObjListIndex].index;

        float xMin = 0;
        float xMax = 0;
        float Y = item.transform.localPosition.y;

        for (int i = 0; i < creatCount; i++)
        {
            Item _item = itemList[i];
            int offset =centerIndex+(i - 2);
            int index = offset;
            if(offset<=0)
            {
                index = _Count + offset;
            }
            else if (offset>_Count)
            {
                index = offset - _Count;
            }

            _item.index = index;
            _item.go.name = i.ToString();

            _item.go.transform.localPosition = new Vector3(i * _item.rect.sizeDelta.x + (_item.rect.sizeDelta.x / 2), Y);
            _item.go.transform.localScale = Vector3.one;

            if (xMin == 0 || _item.rect.transform.localPosition.x < xMin)
            {
                leftIndex = _item.index;
                xMin = _item.rect.transform.localPosition.x;
            }
            if (xMax == 0 || _item.rect.transform.localPosition.x > xMax)
            {
                rightIndex = _item.index;
                xMax = _item.rect.transform.localPosition.x;
            }
            Log($"LoopList ManaRefresh index_:{index} centerIndex:{centerIndex} offset:{offset}");
            _callBackList?.Invoke(_item.go, index, i);    
        }

        CaluteScaleOffset(true);
        CaluteItemHideForLoopListType();

        callBack?.Invoke(itemList[centerObjListIndex].go, itemList[centerObjListIndex].index, centerObjListIndex);
    }

    public void Refresh(int index = 0)
    {
        for (int i = 0; i < creatCount; i++)
        {
            Item _item = itemList[i];
            if (index!=0||index==_item.index)
            {
                _callBackList?.Invoke(_item.go, _item.index, i);
            }
        }
    }

    bool isMoveing = false;
    Coroutine myCoroutine;

    public void MoveToIndex(int index,int objListIndex=-1,Action<GameObject,int,int> callBack = null,Action<GameObject,int,int> moveFailedCallBack=null)
    {
        Log($"LoopList MoveToIndex index:{index} objListIndex:{objListIndex}");

        if (isMoveing)
        {
            return;
        }
        
        isRefreshAllItem = true;

            
        index = index > _Count ? _Count : index;

        index = int_MoveIndex_Test != 0 ? int_MoveIndex_Test : index;

        if (int_MoveIndex_Test!=0)
        {
            //这里是测试的时候用的 注释掉是正确的，  当你需要手动改变移动到的位置时，就把这个代码开放
            //MoveToIndexImmediately(index);
            //return;
        }

        Item _item = itemList[centerObjListIndex];


        float moveDir = 1;
        float moveDistance = 0;
        int moveItemCount = 0;

        switch (Enum_MoveToIndexType)
        {
            case moveToIndexTypeEnum.orderLoop:

                if (itemList[centerObjListIndex].index > index)
                {
                    moveDir = -1;
                }
                moveItemCount = Mathf.Abs(index - _item.index);
                break;
            case moveToIndexTypeEnum.shortDistance:
                int rightDisCount = (index - _item.index + _Count) % _Count;
                int leftDisCount = (_item.index - index + _Count) % _Count;
                moveItemCount = rightDisCount;

                if (leftDisCount < rightDisCount)
                {
                    moveDir = -1;
                    moveItemCount = leftDisCount;
                }
                break;
            default:
                break;
        }

        if (index == -1)
        {
            moveDir = -1;
            moveItemCount = 1;
        }
        else if (index == -2)
        {
            moveDir = 1;
            moveItemCount = 1;
        }

        moveDistance = moveItemCount * _item.rect.rect.width * moveDir - (centerXPos - _item.rect.transform.localPosition.x);

        if (objListIndex>=0 && int_MoveIndex_Test==0)
        {
            moveDistance = itemList[objListIndex].rect.transform.localPosition.x- centerXPos;
        }
        float dis = Mathf.Abs(_item.rect.transform.localPosition.x - centerXPos);
        float scaleOffset = Mathf.Abs(_item.rect.transform.localScale.x - float_Scaling);
        if ((_item.index == index && scaleOffset < Epsilon && dis< Epsilon) || Mathf.Abs(moveDistance)< Epsilon)
        {
            moveFailedCallBack?.Invoke(_item.go, _item.index, _item.index);
            _callBackMoveTargetEnd?.Invoke(_item.go, _item.index, _item.index);
            Log($"LoopList MoveToIndex end moveItemCount:{moveItemCount} moveDistance:{moveDistance}");
            return;
        }
        Log($"LoopList Move moveDistance:{moveDistance}");

        float moveTargetTime = float_SingleMoveItemTargetTime + ((moveItemCount - 1) * float_MoreItemMoveTargetTimeOffset);

        Log($"LoopList MoveToIndex  moveItemCount:{moveItemCount} moveDistance:{moveDistance} moveTargetTime:{moveTargetTime}  index:{index} objListIndex:{objListIndex}");
        
        myCoroutine = StartCoroutine(MoveWithCurveCoroutine(moveDistance, moveTargetTime, callBack));
    }


    // 协程方法，使用AnimationCurve来移动物体
    private IEnumerator MoveWithCurveCoroutine(float targetOffsetX, float moveTargetTime, Action<GameObject,int,int> callBack = null)
    {
        Log("LoopList IEnumerator MoveWithCurveCoroutine");
        float movedX = 0;
        isMoveing = true;
        float elapsedTime = 0f;
        float moveDuration = 0f;
     
        targetOffsetX = targetOffsetX / float_MoveSpeed;

        while (elapsedTime< moveTargetTime)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / moveTargetTime);
            float curveFloat = curve.Evaluate(t);
            moveDuration = Mathf.Lerp(0, targetOffsetX, t* curveFloat);
            MoveList(moveDuration- movedX);
            movedX = moveDuration;

            if (Mathf.Abs(moveDuration) >= Mathf.Abs(targetOffsetX))
            {
                callBack?.Invoke(itemList[centerObjListIndex].go, itemList[centerObjListIndex].index, centerObjListIndex);
                _callBackMoveTargetEnd?.Invoke(itemList[centerObjListIndex].go, itemList[centerObjListIndex].index, centerObjListIndex);
                isMoveing = false;
                yield break;
            }

            yield return null;  // 暂停协程执行，等待下一帧
        }

        if (moveDuration < targetOffsetX)
        {
            moveDuration = targetOffsetX - moveDuration;
            moveDuration = targetOffsetX > 0 ? moveDuration : -moveDuration;
            MoveList(moveDuration);
            callBack?.Invoke(itemList[centerObjListIndex].go, itemList[centerObjListIndex].index, centerObjListIndex);
            _callBackMoveTargetEnd?.Invoke(itemList[centerObjListIndex].go, itemList[centerObjListIndex].index, centerObjListIndex);
            isMoveing = false;
        }

        yield return null; // 等待一帧
    }

    //移动列表
    void MoveList(float x)
    {
        x = -x * float_MoveSpeed;

        switch (Enum_LoopListType)
        {
            case loopListTypeEnum.Loop:
                break;
            case loopListTypeEnum.singleDir:
                float itemX = itemList[centerObjListIndex].rect.transform.localPosition.x;
                if (itemList[centerObjListIndex].index == 1)
                {
                    if (x > 0 && x + itemX > centerXPos)
                    {
                        x = centerXPos - itemX;
                        if (x < Epsilon)
                        {
                            x = 0;
                        }
                    }
                }
                else if (itemList[centerObjListIndex].index == _Count)
                {
                    if (x<0&&x+ itemX< centerXPos)
                    {
                        x = -Mathf.Abs(itemX - centerXPos);
                        if (x< Epsilon)
                        {
                            x = 0;
                        }
                    }
                }

                break;
            default:
                break;
        }

        if (x==0)
        {
            return;
        }

        for (int i = 0; i < creatCount; i++)
        {
            Item _item = itemList[i];

            _item.rect.transform.localPosition = new Vector3(_item.rect.transform.localPosition.x + x, _item.rect.transform.localPosition.y, 0);
            if (-_item.rect.rect.width * 0.5f-_item.rect.transform.localPosition.x>-1)
            {
                //把它放到最右边
                _item.rect.transform.localPosition = new Vector3(_item.rect.transform.localPosition.x + (_item.rect.rect.width * (creatCount)), _item.y, 0);
                int index = rightIndex + 1;
                leftIndex++;

                if (leftIndex>_Count)
                {   
                    leftIndex = 1;
                }

                if (index> _Count)
                {
                    index = 1;
                }
                rightIndex = index;

                _item.index = index;
                isRefreshAllItem = true;

                _item.go.gameObject.SetActive(true);
                int centerObjIndex = centerObjListIndex + 1;
                centerObjIndex = centerObjIndex > (creatCount - 1) ? 0 : centerObjIndex;
                centerObjListIndex = centerObjIndex;
                CaluteItemHideForLoopListType(centerObjIndex);
            }
            else if (_item.rect.rect.width * (creatCount - 1 + 1.5f)- _item.rect.transform.localPosition.x <1 )
            {
                //把它放到最左边
                _item.rect.transform.localPosition = new Vector3(_item.rect.transform.localPosition.x - (_item.rect.rect.width * (creatCount)), _item.y, 0);
                rightIndex--;

                if (rightIndex<1)
                {
                    rightIndex = _Count;
                }

                int index = leftIndex - 1;
                if (index<1)
                {
                    index = _Count;
                }
                leftIndex = index;

                _item.index = index;
                isRefreshAllItem = true;
                _item.go.gameObject.SetActive(true);
                int centerObjIndex = centerObjListIndex -1;
                centerObjIndex = centerObjIndex < 0 ? creatCount - 1 : centerObjIndex;
                centerObjListIndex = centerObjIndex;
                CaluteItemHideForLoopListType(centerObjIndex);
            }
            if (isRefreshAllItem)
            {
                _callBackList?.Invoke(_item.go, _item.index,i);
            }
        }
        isRefreshAllItem = false;
        CaluteScaleOffset();
    }

    /// <summary>
    /// 立即移动到指定位置  没有过程
    /// </summary>
    /// <param name="index"></param>
    public void MoveToIndexImmediately(int index)
    {
        Log("LoopList MoveToIndexImmediately");
        int centerIndex = itemList[centerObjListIndex].index;

        float xMin = 0;
        float xMax = 0;
        for (int i = 0; i < creatCount; i++)
        {
            Item _item = itemList[i];
            int index_ = _item.index - centerIndex + index;
            if (index_ <= 0)
            {
                index_ += _Count;
            }

            if (index_ > _Count)
            {
                index_ -= _Count;
            }

            _item.index = index_;

            if (xMin == 0 || _item.rect.transform.localPosition.x < xMin)
            {
                leftIndex = _item.index;
                xMin = _item.rect.transform.localPosition.x;
            }
            if (xMax == 0 || _item.rect.transform.localPosition.x > xMax)
            {
                rightIndex = _item.index;
                xMax = _item.rect.transform.localPosition.x;
            }

            _callBackList?.Invoke(_item.go, index_, i);
        }

        _callBackMoveTargetEnd?.Invoke(itemList[centerObjListIndex].go, itemList[centerObjListIndex].index, centerObjListIndex);

        CaluteItemHideForLoopListType();
    }

    //计算缩放
    private void CaluteScaleOffset(bool isInit=false)
    {
        for (int i = 0; i < creatCount; i++)
        {
            Item _item = itemList[i];

            float x = _item.rect.transform.localPosition.x;

            float dis;
            float offset;
            
            if (x >= centerXPos)
            {
                offset = right - x;
                dis = offset / center;
                if (right - x <= 0)
                {
                    dis = 0;
                }
            }
            else
            {
                offset = x - left;
                dis = offset / center;
                if (x - left <= 0)
                {
                    dis = 0;
                }
            }

            Vector3 newScale = Vector3.one + (Vector3.one * float_Scaling - Vector3.one) * dis;
            _item.rect.localScale = newScale;

            float yOffsetPos = _item.y + (dis * float_YOffset);

            _item.go.transform.localPosition = new Vector3(_item.go.transform.localPosition.x, yOffsetPos);

            float offsetDis = Mathf.Abs(1 - dis);
            
            if (offsetDis< Epsilon || dis==1)
            {
                if (isInit)
                {
                    _callBackMoveTargetEnd?.Invoke(_item.go, _item.index,i+1);
                }

                centerObjListIndex = i;
                Log($"LoopList Move centerObjListIndex:{centerObjListIndex}");
            }
        }
    }

    //计算Item的隐藏
    private void CaluteItemHideForLoopListType(int centerObjIndex=-1)
    {

        Log($"LoopList Move centerObjIndex:{centerObjIndex}");
        centerObjIndex = centerObjIndex == -1 ? centerObjListIndex : centerObjIndex;
        float xCenterItem = itemList[centerObjIndex].rect.transform.localPosition.x;

        switch (Enum_LoopListType)
        {
            case loopListTypeEnum.Loop:
                break;
            case loopListTypeEnum.singleDir:
                int showCount = 0;
                for (int i = 0; i < creatCount; i++)
                {
                    Item _item = itemList[i];
                    float xItem = _item.rect.transform.localPosition.x;

                    if ((_item.index > itemList[centerObjIndex].index && xItem < xCenterItem)|| (_item.index < itemList[centerObjIndex].index && xItem>xCenterItem))
                    {
                        _item.go.SetActive(false);
                    } else
                    {
                        showCount++;
                            
                        _item.go.SetActive(true);
                    }

                    Log($"LoopList CaluteItemHideForLoopListType  name:{_item.go.name}  _item.Index:{_item.index} centerIndex:{itemList[centerObjListIndex].index} xItem:{xItem} xCenterItem：{xCenterItem}");
                }
                break;
            default:
                break;
        }
    }

    void CreatItem()
    {
        Log("LoopList CreatItem");

        if (_Count==0)
        {
            item.gameObject.SetActive(false);
            return;
        }

        creatCount = (int_ViewCount % 2 != 0) ? int_ViewCount : int_ViewCount + 1;
        creatCount += 2;

        float contentWidth = item.rect.width * creatCount;
        float contentX = -(((creatCount - int_ViewCount) / 2) * item.rect.width);

        content.sizeDelta = new Vector2(contentWidth, content.sizeDelta.y);
        content.transform.localPosition = new Vector3(contentX, content.transform.localPosition.y);

        int indexOffsetLeft = _Count;
        int indexOffsetRight = 1;

        left = item.rect.width * (float_LeftChangePointValue + float_ItemChangeLineOffset);
        right = item.rect.width * (float_RightChangePointValue - float_ItemChangeLineOffset);
        center = (right - left) / 2;

        centerXPos = item.rect.width * (creatCount * 0.5f);

        int childrenCount = content.transform.childCount;

        itemList.Clear();

        for (int i = 0; i < creatCount; i++)
        {
            float itemY = item.transform.localPosition.y;

            Transform children = i< childrenCount? content.transform.GetChild(i):null;
            GameObject go = children !=null? children.gameObject : GameObject.Instantiate(item.gameObject);
            RectTransform rect = go.GetComponent<RectTransform>();
            go.transform.SetParent(content.transform);
            go.name = i.ToString();
            go.transform.localPosition = new Vector3(i * rect.sizeDelta.x + (rect.sizeDelta.x / 2), itemY);
            go.transform.localScale = Vector3.one;
            go.gameObject.SetActive(true);
            Log($"LoopList CreatItem go's HashCode :{go.GetHashCode()}");
            int index = i + 1;
            if (i==int_ViewStartPos)
            {
                index = 1;
            }
            else if (i< int_ViewStartPos)
            {
                index = _Count - (int_ViewStartPos - i) + 1;
                if (index<1)
                {
                    index = indexOffsetLeft--;
                    if (indexOffsetLeft<1)
                    {
                        indexOffsetLeft = _Count;
                    }
                }
            }
            else if (i> int_ViewStartPos)
            {
                index = 1 + (i - int_ViewStartPos);
                if (index>_Count)
                {
                    index = indexOffsetRight++;
                    if (indexOffsetRight>_Count)
                    {
                        indexOffsetRight = 1;
                    }
                }
            }

            Item _item = new Item()
            {
                go = go,
                rect = go.GetComponent<RectTransform>(),
                y = go.transform.localPosition.y,
                index = index
            };

            _callBackList?.Invoke(go, _item.index,i);
            itemList.Add(_item);
        }

        if (childrenCount>itemList.Count)
        {
            for (int i = itemList.Count; i < childrenCount; i++)
            {
                Transform children = i < childrenCount ? content.transform.GetChild(i) : null;
                children.gameObject.SetActive(false);
            }
        }

        leftIndex = itemList[0].index;
        rightIndex = itemList[creatCount - 1].index;

        CaluteScaleOffset(true);

        CaluteItemHideForLoopListType();
    }

    class Item
    {
        public GameObject go;
        public RectTransform rect;
        public float y;
        public int index;
    }


    void Log(string str)
    {
        if (bool_IsDebug)
        {
            Debug.Log(str);
        }
    }


    private enum loopListTypeEnum
    {
        /// <summary>
        /// 循环
        /// </summary>
        Loop,
        /// <summary>
        /// 单向
        /// </summary>
        singleDir,
    }

    private enum moveToIndexTypeEnum
    {

        /// <summary>
        /// 顺序移动
        /// </summary>
        orderLoop,

        /// <summary>
        /// 距离最近
        /// </summary>
        shortDistance,
    };
}