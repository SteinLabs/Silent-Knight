﻿using UnityEngine;

namespace SilentKnight.DungeonGeneration
{
    /// <summary>
    /// Function used for various dungeon tests, and for discovering valid levels.
    /// </summary>
    public class DungeonTest : MonoBehaviour
    {
        /// Variables for level discovery
        [SerializeField] private int m_iterations;
        [SerializeField] private int m_start;
        [SerializeField] private bool m_fabricate;
        [SerializeField] private int m_platforms = 10;
        [SerializeField] private int m_nodes = 2;
        [SerializeField] private float m_wait = 0.5f;

        void Start()
        {
            GetComponent<DungeonRequestManager>().DiscoverValidLevels(m_iterations, m_start, m_fabricate, m_platforms, m_nodes, m_wait);
        }
    }
}