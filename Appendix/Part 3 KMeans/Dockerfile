# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0  
WORKDIR /Artefact3
COPY FORTRAN/*.* ./FORTRAN/

COPY numpy ./numpy/
COPY ILNumerics/*.* ./ILNumerics/

WORKDIR /Artefact3/ILNumerics

RUN dotnet restore Part3.csproj
RUN dotnet publish Part3.csproj -c Release -o /Artefact3/ILNumerics/publish/Release/net8.0/ --no-restore 

RUN apt-get update && \
    apt-get install -y --no-install-recommends gfortran python3 python3-pip python3-venv build-essential libgdiplus && \
    rm -rf /var/lib/apt/lists/*

RUN python3 -m venv /opt/venv
ENV PATH="/opt/venv/bin:$PATH"

RUN /opt/venv/bin/pip install --upgrade pip && \
    /opt/venv/bin/pip install numpy numba

WORKDIR /Artefact3/FORTRAN
RUN mkdir -p ./x64/Release && \
    gfortran -O3 -fPIC -shared -cpp -DOPTIM \
    csv_reader.f90 Part3_KMeans.f90 \
    -o ./x64/Release/Part3_KMeans.so

WORKDIR /Artefact3/ILNumerics/publish/Release/net8.0/

ENTRYPOINT ["dotnet", "Part3.dll"]