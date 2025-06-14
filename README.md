﻿### asv-mavlink-DroneControllerApp

## Drone Controller App
This is a C# console application for discovering, connecting to, and controlling a MAVLink-compatible drone over TCP. It supports automatic drone detection, takeoff, flying to a specific GPS location, and landing.

## Features
Automatic discovery of MAVLink drones

MAVLink v2 protocol support

Position tracking with live logging

Drone control: GUIDED mode, takeoff, go-to-point, and landing

Clean architecture with dependency injection and async disposal

## How to Run
Start your drone or simulator and ensure it's broadcasting MAVLink data to 127.0.0.1:5760.

### The app will:

Connect to the router

Discover the drone

Wait for a valid heartbeat

Switch to GUIDED mode

Take off and optionally fly to a target coordinate
