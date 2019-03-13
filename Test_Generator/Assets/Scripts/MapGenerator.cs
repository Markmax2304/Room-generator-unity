using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public bool isMapGenerated = false;
    public List<Room> rooms;
    public int[,] matrixMap;


    [SerializeField] bool useRandomSeed = false;
    [SerializeField] string seed = "seed";

    [SerializeField] int radius = 20;
    [SerializeField] int amountLargeRooms = 10;
    [SerializeField] int amountUsualRooms = 30;
    [SerializeField] int minSizeUsualRoom = 3;
    [SerializeField] int maxSizeUsualRoom = 6;
    [SerializeField] int minSizeLargeRoom = 3;
    [SerializeField] int maxSizeLargeRoom = 6;

    [SerializeField] GameObject roomPrefab;

    bool isSepareting = false;
    Dictionary<int, Rigidbody2D> rigidbodyRooms;

    void Start()
    {
        if (useRandomSeed)
            Random.InitState(seed.GetHashCode());
        StartGenerateMap();
    }

    void FixedUpdate()
    {
        if (isSepareting && CheckMovingRoomStopped())
        {
            isSepareting = false;
            OffsetRoomsToCorrectPosition();
            CreateMatrixMap();
            DeleteAloneRooms();
            Debug.Log("Complete");
            isMapGenerated = true;
        }
    }

    public void StartGenerateMap()
    {
        RoomsGenerator();
        CreateCollidersForRooms();
    }

    void CreateCollidersForRooms()
    {
        isSepareting = true;
        rigidbodyRooms = new Dictionary<int, Rigidbody2D>();
        for(int i = 0; i < rooms.Count; i++)
        {
            Room temp = rooms[i];
            Transform tempRoom = Instantiate(roomPrefab, temp.centrPos, Quaternion.identity).transform;
            tempRoom.GetComponent<BoxCollider2D>().size = new Vector2(temp.width, temp.height);
            Rigidbody2D tempRig = tempRoom.GetComponent<Rigidbody2D>();
            tempRig.mass = temp.size == SizeRoom.Large ? 10 : 1;
            rigidbodyRooms.Add(temp.id, tempRig);
        }
    }

    bool CheckMovingRoomStopped()
    {
        for(int i = 0; i < rigidbodyRooms.Count; i++)
        {
            if (!rigidbodyRooms[i].IsSleeping())
                return false;
        }

        return true;
    }

    void OffsetRoomsToCorrectPosition()
    {
        foreach(KeyValuePair<int, Rigidbody2D> rig in rigidbodyRooms)
        {
            rig.Value.transform.GetComponent<BoxCollider2D>().enabled = false;
        }

        foreach(KeyValuePair<int, Rigidbody2D> rigid in rigidbodyRooms)
        {
            Room tempRoom = rooms.Find(p => p.id == rigid.Key);
            Vector3 position = rigid.Value.transform.position;

            if (tempRoom.width % 2 == 0)
                position.x = Mathf.Floor(position.x) + .5f;
            else
                position.x = Mathf.Round(position.x);

            if (tempRoom.height % 2 == 0)
                position.y = Mathf.Floor(position.y) + .5f;
            else
                position.y = Mathf.Round(position.y);
           
            int n = rooms.IndexOf(tempRoom);
            tempRoom.SetCentrPos(position);
            rooms[n] = tempRoom;
        }

        for(int i = 0; i < rigidbodyRooms.Count; i++)
        {
            Destroy(rigidbodyRooms[i].gameObject);
        }
        rigidbodyRooms.Clear();
    }

    void CreateMatrixMap()
    {
        int minX = (int)rooms[0].pos.x, maxX = (int)rooms[0].pos.x + rooms[0].width - 1;
        int minY = (int)rooms[0].pos.y, maxY = (int)rooms[0].pos.y + rooms[0].height - 1;
        for (int i = 1; i < rooms.Count; i++)
        {
            int minTempX = (int)rooms[i].pos.x, maxTempX = (int)rooms[i].pos.x + rooms[i].width - 1;
            int minTempY = (int)rooms[i].pos.y, maxTempY = (int)rooms[i].pos.y + rooms[i].height - 1;

            if (minX > minTempX)
                minX = minTempX;
            if (maxX < maxTempX)
                maxX = maxTempX;

            if (minY > minTempY)
                minY = minTempY;
            if (maxY < maxTempY)
                maxY = maxTempY;
            //Debug.Log(minX + "  " + minY + "  " + maxX + "  " + maxY);
        }
        //Debug.DrawLine(new Vector3(minX, minY, 0), new Vector3(maxX, maxY, 0), Color.red, 10f);

        int height = maxY - minY;
        int width = maxX - minX;
        Room.offset = new Vector2(Mathf.Abs(minX), Mathf.Abs(minY));
        matrixMap = new int[height + 1, width + 1];
        for(int i = 0; i < matrixMap.GetLength(0); i++)
        {
            for (int j = 0; j < matrixMap.GetLength(1); j++)
            {
                matrixMap[i, j] = -1;
            }
        }

        for(int i = 0; i < rooms.Count; i++)
        {
            AddRoomToMatrix(rooms[i]);
        }
    }

    void DeleteAloneRooms()
    {
        for(int i = 0; i < rooms.Count; i++)
        {
            Vector2 startPos = rooms[i].GetPositionInMatrix();
            int touchCounter = 0;
            for (int y = (int)startPos.y - 1; y < (int)startPos.y + rooms[i].height + 1; y++)
            {
                for (int x = (int)startPos.x - 1; x < (int)startPos.x + rooms[i].width + 1; x++)
                {
                    if (x >= 0 && x < matrixMap.GetLength(1) && y >= 0 && y < matrixMap.GetLength(0))
                    {
                        if (x == (int)startPos.x - 1 || x == (int)startPos.x + rooms[i].width || y == (int)startPos.y - 1 || y == (int)startPos.y + rooms[i].height)
                        {
                            if (matrixMap[y, x] != -1)
                                touchCounter++;
                        }
                    }
                }
            }

            if (touchCounter < 4)
                DeleteRoomFromMatrix(rooms[i]);
        }
    }

    void AddRoomToMatrix(Room room)
    {
        Vector2 startPos = room.GetPositionInMatrix();
        for (int y = (int)startPos.y; y < (int)startPos.y + room.height; y++)
        {
            for (int x = (int)startPos.x; x < (int)startPos.x + room.width; x++)
            {
                matrixMap[y, x] = room.id;
            }
        }
    }

    void DeleteRoomFromMatrix(Room room)
    {
        //Debug.Log(room.id);
        Vector2 startPos = room.GetPositionInMatrix();
        for (int y = (int)startPos.y; y < (int)startPos.y + room.height; y++)
        {
            for (int x = (int)startPos.x; x < (int)startPos.x + room.width; x++)
            {
                matrixMap[y, x] = -1;
            }
        }
    }

    void OnDrawGizmos()
    {
        if (matrixMap != null)
        {
            for (int x = 0; x < matrixMap.GetLength(0); x++)
            {
                for (int y = 0; y < matrixMap.GetLength(1); y++)
                {
                    if (matrixMap[x, y] == -1)
                        continue;

                    float attrColor = matrixMap[x, y] / (float)rooms.Count;
                    Gizmos.color = new Color(attrColor, attrColor * attrColor, attrColor * attrColor * attrColor);
                    Vector3 pos = new Vector3(x, y, 0);
                    Gizmos.DrawCube(pos, Vector3.one * .9f);
                }
            }
        }
    }

    //Generate room
    void RoomsGenerator()
    {
        rooms = new List<Room>();
        
        for (int i = 0; i < amountLargeRooms; i++)
        {
            Vector2 pos = GetRoomsPointInCircle(radius);
            int height = Random.Range(minSizeLargeRoom, maxSizeLargeRoom + 1);
            int width = Random.Range(minSizeLargeRoom, maxSizeLargeRoom + 1);
            Room room = new Room(height, width, pos, i, SizeRoom.Large);
            rooms.Add(room);
        }
        for (int i = 0; i < amountUsualRooms; i++)
        {
            Vector2 pos = GetRoomsPointInCircle(radius);
            int height = Random.Range(minSizeUsualRoom, maxSizeUsualRoom + 1);
            int width = Random.Range(minSizeUsualRoom, maxSizeUsualRoom + 1);
            Room room = new Room(height, width, pos, i + amountLargeRooms, SizeRoom.Usual);
            rooms.Add(room);
        }
    }

    Vector2 GetRoomsPointInCircle(int radius)
    {
        float t = 2 * Mathf.PI * Random.value;
        float u = Random.value + Random.value;
        float r = 0;
        if (u > 1) r = 2 - u;
        else r = u;
        int x = (int)(radius * r * Mathf.Cos(t));
        int y = (int)(radius * r * Mathf.Sin(t));
        return new Vector2(x, y);
    }
}

public enum SizeRoom { Usual, Large};

public struct Room
{
    public static Vector2 offset;
    public int height;
    public int width;
    public Vector2 pos;
    public Vector2 centrPos;
    public int id;
    public SizeRoom size;

    public Room(int _height, int _width, Vector2 _pos, int _id, SizeRoom _size)
    {
        height = _height;
        width = _width;
        pos = _pos;
        id = _id;
        size = _size;
        centrPos = _pos + new Vector2(_width / 2f - .5f, _height / 2f - .5f);
    }

    public void SetCentrPos(Vector2 point)
    {
        pos = point - new Vector2(width / 2f - .5f, height / 2f - .5f);
        centrPos = point;
    }

    public Vector2 GetPositionInMatrix()
    {
        return pos + offset;
    }
}
