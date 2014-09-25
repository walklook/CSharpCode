#define LOAD_ADDITIVE_ASYNC

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

public class TileNavigation : MonoBehaviour
{
	public struct TerrainRecord
	{
		public int col;
		public int row;
		
		public TerrainRecord( int col, int row )
		{
			this.col = col;
			this.row = row;
		}
	}
	
	struct TerrainAttachment
	{
		public GameObject terrain;
		public List<GameObject> attachments;
	}
	
	public struct TerrainTask
	{
		public int col;
		public int row;
		public string terrainName;
	}
	
	public const float TILE_SIZE = 512.0f; //2000.0f;
    public const string TERRAIN_NAME = "Terrain_sand_1024_test_128hightmap_resolution_Slice_";
    public const string SCENE_NAME = "Terrain_sand_";
	
	private float mHalfFOV = 30.0f;
	private float mCameraMinForward;
	private float mCameraMaxForward;
	private float mHalfHorizontalFOV;
	private int mCameraCol;
	private int mCameraRow;
	
	public static float sTopSide;
	public static float sBottomSide;
	public static float sLeftSide;
	public static float sRightSide;
	
	public static List<TerrainTask> sTerrainTasks = new List<TerrainTask>();
	public static List<TerrainRecord> sTerrainRecords;
	private List<TerrainAttachment> mTerrainAttachments;
	private int mTerrainNum = 1;
	
	// Use this for initialization
	void Start()
	{
		mHalfFOV = camera.fieldOfView * 0.5f;
		mHalfHorizontalFOV = mHalfFOV * camera.aspect;
		mHalfHorizontalFOV = Mathf.Deg2Rad * mHalfHorizontalFOV;
		mCameraMinForward = Vector3.Angle( camera.transform.forward, -Vector3.up );
		mCameraMinForward -= mHalfFOV;
		mCameraMinForward = Mathf.Deg2Rad * mCameraMinForward;
		mCameraMaxForward = mCameraMinForward + Mathf.Deg2Rad * camera.fov;
		
		sTerrainRecords = new List<TerrainRecord>();
		TerrainRecord tr = new TerrainRecord();
		UpdateCameraTile();
		tr.col = mCameraCol;
		tr.row = mCameraRow;
		mTerrainAttachments = new List<TerrainAttachment>();
		TerrainAttachment ta = new TerrainAttachment();
		ta.terrain = GameObject.Find( "Terrain_original" );
		ta.attachments = new List<GameObject>( 4 );
		//tr.attachments.Add( GameObject.Find( "Daylight Water" ) );
		//tr.attachments.Add( GameObject.Find( "bird1" ) );
		//tr.attachments.Add( GameObject.Find( "bird2" ) );
		sTerrainRecords.Add( tr );
		mTerrainAttachments.Add( ta );
	}
	
	// Update is called once per frame
	void Update()
	{	
		UpdateCameraTile();
		
		Vector3 cameraPosition = camera.transform.position;
		float viewHeight1 = cameraPosition.y * Mathf.Tan( mCameraMinForward );
		float viewHeight2 = cameraPosition.y * Mathf.Tan( mCameraMaxForward );
		float viewWidth = cameraPosition.y / Mathf.Cos( mCameraMaxForward ) * Mathf.Tan( mHalfHorizontalFOV );
		
		float viewHeightCompensator1 = 40.0f;
		float viewHeightCompensator2 = 40.0f;
		float viewWidthCompensator = 40.0f;
		
		sTopSide = cameraPosition.x - viewHeight2 - viewHeightCompensator2 - TILE_SIZE;
		sBottomSide = cameraPosition.x - viewHeight1 + viewHeightCompensator1 + TILE_SIZE;
		sLeftSide = cameraPosition.z - viewWidth - viewWidthCompensator - TILE_SIZE;
		sRightSide = cameraPosition.z + viewWidth + viewWidthCompensator + TILE_SIZE;
		
		/*//////////////////
		        |
		 -1, 1	|  1, 1
				|
		-----------------
				|
		 -1, -1	|  1, -1
				|
		//////////////////*/
		
		TerrainRecord leftTopTile = GetTileInfo( cameraPosition.x - viewHeight2 - viewHeightCompensator2, cameraPosition.z - viewWidth - viewWidthCompensator );
		TerrainRecord rightTopTile = GetTileInfo( cameraPosition.x - viewHeight2 - viewHeightCompensator2, cameraPosition.z + viewWidth + viewWidthCompensator );
		TerrainRecord leftBottomTile = GetTileInfo( cameraPosition.x - viewHeight1 + viewHeightCompensator1, cameraPosition.z - viewWidth - viewWidthCompensator );
		TerrainRecord rightBottomTile = GetTileInfo( cameraPosition.x - viewHeight1 + viewHeightCompensator1, cameraPosition.z + viewWidth + viewWidthCompensator );
		
		for ( int row = leftTopTile.row; row >= leftBottomTile.row; row-- )
		{
			if ( row == 0 ) continue;
			
			bool isBreak = false;
			for ( int col = leftTopTile.col; col <= rightTopTile.col; col++ )
			{
				if ( col == 0 ) continue;
				
				if ( !IsExistInScene( col, row ) )
				{
                    Debug.Log( "add terrain at row = " + row + ", col = " + col );
					StartCoroutine( AddTerrainCoroutine( col, row ) );
					isBreak = true;
				}
			}
			
			if ( isBreak )
			{
				// add more tiles next frame
				break;
			}
		}
		
	}
	
	bool IsExistInScene( int col, int row )
	{
		for ( int i = 0; i < sTerrainRecords.Count; i++ )
		{
			TerrainRecord tr = (TerrainRecord)sTerrainRecords[i];
			if ( tr.col == col && tr.row == row )
			{
				return true;
			}
		}
		
		return false;
	}
	
	TerrainRecord GetTileInfo( float x, float z )
	{
		float rowX = x > 0 ? x + TILE_SIZE : x - TILE_SIZE;
		int row = -(int)( rowX / TILE_SIZE );
		float colZ = z > 0 ? z + TILE_SIZE : z - TILE_SIZE;
		int col = (int)( colZ / TILE_SIZE );
		
		return new TerrainRecord( col, row );
	}
	
	void UpdateCameraTile()
	{
		Vector3 cameraPosition = camera.transform.position;
		float x = cameraPosition.x > 0 ? cameraPosition.x + TILE_SIZE : cameraPosition.x - TILE_SIZE;
		mCameraRow = -(int)( x / TILE_SIZE );
		float z = cameraPosition.z > 0 ? cameraPosition.z + TILE_SIZE : cameraPosition.z - TILE_SIZE;
		mCameraCol = (int)( z / TILE_SIZE );
	}
	
	IEnumerator AddTerrainCoroutine( int col, int row )
	{
		mTerrainNum++;
		
        string terrainName = "";
        string sceneName = "";
		GetTerrainName( ref terrainName, ref sceneName, col, row );
        Debug.Log( "terrain name = " + terrainName + ", scene name = " + sceneName );

#if LOAD_ADDITIVE_ASYNC
		lock ( sTerrainRecords )
		{
			TerrainRecord tr = new TerrainRecord();
			tr.col = col;
			tr.row = row;
			sTerrainRecords.Add( tr );
            Debug.Log( "add terrain record col = " + col + ", row = " + row );
		}
		TerrainAttachment ta = new TerrainAttachment();
		// can't add terrain now......
		ta.attachments = new List<GameObject>( 4 );
		mTerrainAttachments.Add( ta );
		
		lock ( sTerrainTasks )
		{
			TerrainTask tt = new TerrainTask();
			tt.col = col;
			tt.row = row;
			tt.terrainName = terrainName;
			sTerrainTasks.Add( tt );
		}
		AsyncOperation async = Application.LoadLevelAdditiveAsync( sceneName );
        yield return async;
        Debug.Log("Loading complete");
#else
		float z = ( col > 0 ? col - 1 : col ) * TILE_SIZE;
		float x = -( row < 0 ? row + 1 : row ) * TILE_SIZE;
		
		Object t = Resources.Load( terrainName );
		TerrainRecord tr = new TerrainRecord();
		tr.col = col;
		tr.row = row;
		sTerrainRecords.Add( tr );
		TerrainAttachment ta = new TerrainAttachment();
		yield return ta.terrain = Instantiate( t, new Vector3( x, 0, z ), Quaternion.identity );
		ta.attachments = new List<GameObject>( 4 );
		
		t = null;
		mTerrainAttachments.Add( ta );
#endif
		//Resources.UnloadUnusedAssets();
	}
	
	void GetTerrainName( ref string terrainName, ref string sceneName, int col, int row )
	{
        Debug.Log( "GetTerrainName before : col = " + col + ", row = " + row );
		if ( col % 2 == 0 )
		{
			if ( col > 0 )
			{
				col = 2;
			}
			else
			{
				col = 1;
			}
		}
		else
		{
			if ( col > 0 )
			{
				col = 1;
			}
			else
			{
				col = 2;
			}
		}
		
		if ( row % 2 == 0 )
		{
			if ( row > 0 )
			{
				row = 1;
			}
			else
			{
				row = 2;
			}
		}
		else
		{
			if ( row > 0 )
			{
				row = 2;
			}
			else
			{
				row = 1;
			}
		}
		
        Debug.Log( "GetTerrainName after : col = " + col + ", row = " + row );
		
        terrainName = TERRAIN_NAME + col + "_" + row;
        sceneName = SCENE_NAME + col + "_" + row;
		
	}
	
}
