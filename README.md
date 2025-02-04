# Variable Offset

A lightweight Grasshopper plugin for generating variable offsets from polylines, designed specifically for urban-scale setback analysis and planning regulation studies. The tool takes a polyline input and a corresponding list of offset distances to generate offset geometry with different distances for each segment.

## Key Features

* Fast processing (~0.02ms per polygon) optimized for urban-scale analysis
* Simple interface: input polyline/polygon and list of offset distances 
* Handles both inward and outward offsets
* Basic cleanup of self-intersections (this is not perfect and will improve over time)
* Suitable for batch processing large datasets

## Common Applications

* Urban setback analysis
* Planning regulation compliance checks
* Parcel development envelope studies
* Building footprint generation

## Limitations

* Self-intersections may occur with complex variable offset configurations
* Basic intersection cleanup may not handle all edge cases perfectly

## Requirements

* Rhinoceros 8 or later
* Grasshopper

## Getting Started

* Component can be found under Crv>Util
* Input your boundary polyline
* Provide a list of offset distances corresponding to each segment
* Connect to the component to generate the variable offset result