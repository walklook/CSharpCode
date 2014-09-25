using UnityEngine;
using System.Collections;

public class Tileself : MonoBehaviour
{ 
	int mCol = 1;
	int mRow = -1;
	
	// Use this for initialization
	void Start ()
	{
		lock( TileNavigation.sTerrainTasks )
		{
			for ( int i = 0; i < TileNavigation.sTerrainTasks.Count; i++ )
			{
				TileNavigation.TerrainTask tt = (TileNavigation.TerrainTask)TileNavigation.sTerrainTasks[i];
				if ( tt.terrainName == gameObject.name )
				{
					mCol = tt.col;
					mRow = tt.row;
					
					float z = ( mCol > 0 ? mCol - 1 : mCol ) * TileNavigation.TILE_SIZE;
					float x = -( mRow < 0 ? mRow + 1 : mRow ) * TileNavigation.TILE_SIZE;
					gameObject.transform.position = new Vector3( x, 0, z );
					TileNavigation.sTerrainTasks.RemoveAt( i );
					break;
				}
			}
		}
		
	}
	
	// Update is called once per frame
	void Update ()
	{
		bool isDelete = false;
		if ( gameObject.transform.position.x < TileNavigation.sTopSide )
		{
			isDelete = true;
		}
		else if ( gameObject.transform.position.x > TileNavigation.sBottomSide )
		{
			isDelete = true;
		}
		else if ( gameObject.transform.position.z < TileNavigation.sLeftSide )
		{
			isDelete = true;
		}
		else if ( gameObject.transform.position.z > TileNavigation.sRightSide )
		{
			isDelete = true;
		}
		
		if ( isDelete )
		{
			int i = 0;
			lock ( TileNavigation.sTerrainRecords )
			{
				for ( ; i < TileNavigation.sTerrainRecords.Count; i++ )
				{
					TileNavigation.TerrainRecord tr = TileNavigation.sTerrainRecords[i];
					if ( tr.col == mCol && tr.row == mRow )
					{
						TileNavigation.sTerrainRecords.RemoveAt( i );
						break;
					}
				}
			}
			
			// Because the purpose of multithread loading terrain is for memory limit, so don't cache them. besides this, every terrain object may have different state in games.	
			DestroyImmediate( gameObject );
			Resources.UnloadUnusedAssets();
		}
	}
}
