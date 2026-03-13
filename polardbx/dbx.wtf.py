def get_container_ip(container):
    attrs = container.attrs or {}
    ns = attrs.get("NetworkSettings") or {}

    ip = ns.get("IPAddress")
    if ip:
        return ip

    networks = ns.get("Networks") or {}
    for _, net in networks.items():
        ip = net.get("IPAddress")
        if ip:
            return ip

    raise KeyError("No container IP found in Docker NetworkSettings")


# Dosłownie musiałem modyfikować kod, żeby dodać obsługę tego przypadku, bo niektóre kontenery mają IP w innym miejscu. 
# 'container_ip': container.attrs['NetworkSettings']['IPAddress'],
# 'container_ip': get_container_ip(container),